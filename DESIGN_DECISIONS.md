# Payment Gateway:  Design Decisions & Architecture

> **Take-home exercise** :-  a payment gateway that processes card payments through an acquiring bank and allows merchants to retrieve previous payment details.

---

## Table of Contents

1. [Requirements](#requirements)
2. [Architecture Overview](#architecture-overview)
3. [Request Lifecycle](#request-lifecycle)
4. [Key Design Decisions](#key-design-decisions)
5. [Initial Approach vs Final Approach](#initial-approach-vs-final-approach)
6. [Validation Strategy](#validation-strategy)
7. [Security & PCI-DSS](#security--pci-dss)
8. [Error Handling](#error-handling)
9. [Observability](#observability)
10. [Testing Strategy](#testing-strategy)
11. [Assumptions](#assumptions)
12. [If I Had More Time](#if-i-had-more-time)
13. [Project Structure](#project-structure)

---

## Requirements

| Requirement | Implementation |
|---|---|
| POST endpoint to process payment | `POST /api/payments` via Minimal API |
| GET endpoint to retrieve payment by ID | `GET /api/payments/{id:guid}` via Minimal API |
| Card number: 14–19 numeric characters | FluentValidation: `Length(14, 19)` + `Must(BeNumericOnly)` |
| Expiry month: 1–12 | FluentValidation: `InclusiveBetween(1, 12)` |
| Expiry date: must not be in the past | TimeProvider-injected expiry check (last day of month) |
| Currency: ISO 3-letter, subset supported | `SupportedCurrencies` whitelist: USD, EUR, GBP |
| Amount: positive integer (minor units) | FluentValidation: `GreaterThan(0)`, `long` type |
| CVV: 3–4 numeric digits | FluentValidation: `Length(3, 4)` + `Must(BeNumericOnly)` |
| Bank simulator integration | `BankClient` with `HttpClientFactory`, snake_case contract |
| Response includes payment ID | `Guid` generated in `Payment.Create()` factory |
| Response includes status | `Authorized`, `Declined`, or `Rejected` |
| Response includes last four card digits | `CardNumberLastFour` — only last four ever stored |
| Response includes expiry month/year | Stored as `int` — formatted as `D2` for bank only |
| Response includes currency & amount | `Money` value object with currency normalisation |
| GET response masks card number | Domain only stores last four; full PAN never persisted |
| 400 for validation failures | `ValidationBehavior` pipeline → structured field errors |
| 502 for bank unavailability | Bank 503, network error, timeout → `502 Bad Gateway` |
| 404 for payment not found | `GetPaymentHandler` returns `Errors.PaymentNotFound(id)` |

---

## Architecture Overview

### High-Level Architecture

```
┌──────────────────┐
│  Merchant Client │
└────────┬─────────┘
         │ HTTP POST/GET
         ▼
┌──────────────────────────────────────────────────────────┐
│  Payment Gateway (.NET 8)                                │
│                                                          │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ Middleware: CorrelationId · GlobalExceptionHandler  │ │
│  └──────────────────────┬──────────────────────────────┘ │
│                         ▼                                │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ Minimal API Endpoints                               │ │
│  │   POST /api/payments    GET /api/payments/{id}      │ │
│  └──────────────────────┬──────────────────────────────┘ │
│                         │ IMediator.Send()               │
│                         ▼                                │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ MediatR Pipeline                                    │ │
│  │   LoggingBehavior → ValidationBehavior → Handler    │ │
│  └──────────┬───────────────────────────┬──────────────┘ │
│             │                           │                │
│             ▼                           ▼                │
│  ┌──────────────────┐        ┌────────────────────┐      │
│  │ ProcessPayment   │        │ GetPayment         │      │
│  │ Handler          │        │ Handler            │      │
│  └───┬──────────┬───┘        └────────┬───────────┘      │
│      │          │                     │                  │
│      ▼          ▼                     ▼                  │
│  ┌────────┐  ┌──────────────────────────────────┐        │
│  │ Bank   │  │ Domain: Payment · Money · Status │        │
│  │ Client │  └──────────────────────────────────┘        │
│  └───┬────┘  ┌──────────────────────────────────┐        │
│      │       │ InMemoryPaymentRepository        │        │
│      │       │ (ConcurrentDictionary)           │        │
│      │       └──────────────────────────────────┘        │
└──────┼───────────────────────────────────────────────────┘
       │ POST /payments
       ▼
┌──────────────────┐
│  Bank Simulator  │
│  Mountebank :8080│
└──────────────────┘
```

### Vertical Slice Architecture

```
 ┌─── Feature: Process Payment ──────────────────────────────────────────┐
 │                                                                       │
 │  Request DTO → Command → Validator → Handler → Response DTO           │
 │                                                                       │
 │  ProcessPaymentRequest                                                │
 │       └→ ProcessPaymentCommand                                        │
 │              └→ ProcessPaymentCommandValidator (FluentValidation)     │
 │                     └→ ProcessPaymentCommandHandler (orchestrator)    │
 │                            └→ ProcessPaymentResponse                  │
 └───────────────────────────────────────────────────────────────────────┘

 ┌─── Feature: Get Payment ──────────────────────────────────────────────┐
 │                                                                       │
 │  GetPaymentQuery → GetPaymentQueryHandler → GetPaymentResponse        │
 │                                                                       │
 └───────────────────────────────────────────────────────────────────────┘

 ┌─── Shared Kernel ─────────────────┐    ┌─── Domain ──────────────────┐
 │  Result<T>  (Railway pattern)      │   │  Payment  (Aggregate root)  │
 │  Error / ErrorCodes                │   │  PaymentStatus (enum)       │
 │  Money (value object)              │   │  Money (value object)       │
 │  SupportedCurrencies (whitelist)   │   └─────────────────────────────┘
 └────────────────────────────────────┘
```

---

## Request Lifecycle

### POST /api/payments — Process Payment

```
 Merchant                    Gateway                              Bank Simulator
    │                          │                                       │
    │  POST /api/payments      │                                       │
    │─────────────────────────>│                                       │
    │                          │                                       │
    │                    CorrelationIdMiddleware                       │
    │                    (header → TraceId → GUID)                     │
    │                          │                                       │
    │                    LoggingBehavior                               │
    │                    (start Stopwatch)                             │
    │                          │                                       │
    │                    ValidationBehavior                            │
    │                    (run FluentValidation)                        │
    │                          │                                       │
    │                    ┌─────┴─────┐                                 │
    │                    │ Valid?    │                                 │
    │                    └─────┬─────┘                                 │
    │                     NO   │  YES                                  │
    │<─ 400 + field errors ────┤                                       │
    │                          │                                       │
    │                    ProcessPaymentHandler                         │
    │                    • Extract card last four                      │
    │                    • Format expiry as DD/YYYY                    │
    │                          │                                       │
    │                          │  POST /payments (snake_case)          │
    │                          │──────────────────────────────────────>│
    │                          │                                       │
    │                          │     ┌─────────────────────────────┐   │
    │                          │     │ 200: { authorized, auth_code }   │
    │                          │<────│ 503: Service Unavailable    │────│
    │                          │     │ 400: Bad Request            │    │
    │                          │     └─────────────────────────────┘    │
    │                          │                                        │
    │                    ┌─────┴──────────┐                             │
    │                    │ Bank response? │                             │
    │                    └─────┬──────────┘                             │
    │                   200    │  503/network/timeout    400            │
    │                    │     │         │               │              │
    │                    ▼     │         ▼               ▼              │
    │              Payment.Create  502 Bad Gateway  400 Bad Request     │
    │              repository.Add       │               │               │
    │                    │              │               │               │
    │<── 200 OK ─────────┘              │               │               │
    │<──────────────────────────────────┘               │               │
    │<─────────────────────────────────────────────────┘                │
    │                          │                                        │
    │                    LoggingBehavior                                │
    │                    (log elapsed ms)                               │
```

### GET /api/payments/{id} — Retrieve Payment

```
 Merchant              Gateway                     Repository
    │                     │                            │
    │ GET /payments/{id}  │                            │
    │────────────────────>│                            │
    │                     │   GetById(id)              │
    │                     │───────────────────────────>│
    │                     │                            │
    │                     │   ┌────────────────────┐   │
    │                     │   │ Found?             │   │
    │                     │   └────────┬───────────┘   │
    │                     │      YES   │    NO         │
    │                     │       │    │     │         │
    │<── 200 OK ──────────│<──────┘    │     │         │
    │   (card last four)  │            │     │         │
    │                     │            │     │         │
    │<── 404 Not Found ───│<───────────┘─────┘         │
    │                     │                            │
```

---

## Key Design Decisions

### 1. Vertical Slice Architecture over Layered Architecture

**Decision:** Organise code by feature (`ProcessPayment/`, `GetPayment/`) rather than by technical layer (`Controllers/`, `Services/`, `Repositories/`).

**Reasoning:**
- Each feature is self-contained, request DTO, command, validator, handler, response
- Adding a new feature (e.g., refunds) means adding one folder, not editing five layers
- Reduces the risk of spaghetti dependencies between unrelated features
- Handlers are small, focused, and independently testable

> *Principles: **SRP** each handler has exactly one reason to change; **YAGNI** no shared service layers added speculatively.*

### 2. MediatR CQRS Pipeline

**Decision:** Use MediatR with command/query separation and pipeline behaviours for cross-cutting concerns.

**Reasoning:**
- **Commands** (`ProcessPaymentCommand`) modify state; **queries** (`GetPaymentQuery`) read state
- Pipeline behaviours provide a clean extension point: `Logging → Validation → Handler`
- Validation runs automatically before every handler — impossible to forget
- Adding a new cross-cutting concern (metrics, caching) means registering one more behaviour, not modifying every handler

> *Principles: **OCP** pipeline is open for extension (new behaviours) but closed for modification (no existing handler changes); **DRY** logging and validation written once, applied to every command automatically.*

**Pipeline execution order:**
```
Request → LoggingBehavior (start timer)
        → ValidationBehavior (FluentValidation)
        → Handler (business logic)
        ← ValidationBehavior (short-circuit if invalid)
        ← LoggingBehavior (log elapsed ms)
Response (returned to client)
```

> *Trade-off: for just two endpoints I wouldn’t normally reach for MediatR. I used it here to demonstrate pipeline behaviour and cross-cutting separation and the architecture makes adding a third endpoint (refunds, say) a zero-boilerplate exercise.*

### 3. Railway-Oriented Programming (`Result<T>`)

**Decision:** Return `Result<T>` from all handlers and infrastructure instead of throwing exceptions.

**Reasoning:**
- Payment processing regularly encounters non-exceptional failures (declined cards, bank unavailability) these are expected business outcomes, not program errors
- Makes error paths explicit in method signatures: `Task<Result<T>>`
- Implicit conversions (`T → Result<T>`, `Error → Result<T>`) keep handler code clean
- `Match<TOut>` eliminates unchecked error states at the endpoint layer
- No hidden exception flows, the call chain is transparent

> *Principles: **KISS** no exception hierarchy or catch chains to maintain; **ISP** callers consume only the success or failure path they need via `Match`.*

```csharp
// Handler returns errors via implicit conversion
if (bankResult.IsFailure) return bankResult.Error;  // Error → Result<T>
return ProcessPaymentResponse.FromPayment(payment);  // T → Result<T>

// Endpoint uses Match — compiler ensures both paths are handled
result.Match<IResult>(
    onSuccess: response => Results.Ok(response),
    onFailure: error => MapError(error));
```

### 4. Minimal APIs over Controllers

**Decision:** Use ASP.NET Core Minimal APIs instead of MVC controllers.

**Reasoning:**
- The gateway has exactly two endpoints, a full MVC controller adds unnecessary ceremony
- Minimal APIs are the modern .NET 8 pattern for focused HTTP services
- Lambda based endpoint definitions are concise and co-located
- All orchestration logic lives in MediatR handlers; the endpoint layer is purely HTTP mapping
- `public partial class Program;` provides `WebApplicationFactory` support for integration tests

> *Principles: **YAGNI** no MVC scaffolding, base classes, or action filters for two routes; **KISS** endpoint definitions are plain lambdas, nothing to inherit or configure.*

### 5. Immutable Domain Model

**Decision:** The `Payment` aggregate root is sealed with a private constructor, get-only properties, and factory methods.

**Reasoning:**
- Once a payment is created, its status and card details must never change silently
- Factory methods (`Create`, `CreateRejected`) enforce business invariants at construction time
- `GuardCardLastFour` ensures only exactly 4 digits are stored, impossible to store a full PAN
- Immutability eliminates an entire class of concurrency bugs with the in-memory store

> *Principles: **SRP** `Payment` is solely responsible for enforcing its own invariants; **Encapsulation** private constructor and `Guard` methods make invalid state unrepresentable.*

### 6. `Money` Value Object

**Decision:** Represent amount + currency as a single `Money` value object rather than separate primitives.

**Reasoning:**
- Prevents accidental mixing of currencies (e.g., adding GBP to USD amounts)
- Enforces `Amount > 0` and valid currency at construction time
- `ToUpperInvariant()` normalises currency codes, "gbp" and "GBP" are treated identically
- Domain-Driven Design best practice for any financial system

> *Principles: **DRY** currency normalisation and amount validation live in one place, not scattered across validators and handlers; **SRP** `Money` owns all monetary concerns.*

### 7. `ConcurrentDictionary` for Thread Safety

**Decision:** Use `ConcurrentDictionary<Guid, Payment>` in the in-memory repository.

**Reasoning:**
- The Kestrel web server processes requests concurrently across multiple threads
- A plain `Dictionary` would risk data corruption under concurrent writes
- `TryAdd` and `GetValueOrDefault` are atomic operations, no locking required
- `Singleton` lifetime is correct for an in-memory store (shared state across all requests)

> *Principles: **ISP** `IPaymentRepository` exposes only `Add` and `GetById`; nothing consumers don't need; **DIP** handlers depend on the abstraction, not the concrete `ConcurrentDictionary`.*

### 8. `TimeProvider` Injection

**Decision:** Inject `TimeProvider` into the validator instead of calling `DateTime.UtcNow`.

**Reasoning:**
- Expiry validation depends on "now", tests must control time deterministically
- `FakeTimeProvider` allows pinning to any date (pinned to 2025-06-15 in our tests)
- .NET 8 built-in abstraction, no third-party mocking libraries needed for time
- Avoids intermittent test failures when running near month/year boundaries

> *Principles: **DIP** validator depends on the `TimeProvider` abstraction, not a static `DateTime` call; testability is a first-class concern.*

### 9. Three Currencies Only

**Decision:** Whitelist `USD`, `EUR`, `GBP` via `SupportedCurrencies` rather than accepting all ISO currencies.

**Reasoning:**
- The spec says "Ensuring the currency is a valid ISO code", we interpret this as a strict merchant agreement
- Real payment gateways typically support a configured subset of currencies per merchant
- Easy to extend: just add to the `HashSet` (case-insensitive comparison built in)

> *Principles: **YAGNI** no support for all 180+ ISO currencies until there is a concrete requirement; **KISS** a `HashSet` lookup is simpler and faster than a third-party ISO library.*

---

## Initial Approach vs Final Approach

### Controller → Minimal APIs

| Aspect | Initial Sketch | Final Implementation |
|---|---|---|
| HTTP layer | `PaymentsController : ControllerBase` | Minimal API lambdas in `EndpointMappingExtensions` |
| Why changed | MVC controllers add attribute routing, model binding, and base class ceremony for just two endpoints | MediatR handles all orchestration; endpoints are only responsible for HTTP mapping |
| Trade-off | Controllers offer built-in model validation via `[ApiController]` | We get cleaner code at the cost of manual `MapError()` switch |

### Scattered Validation → MediatR Pipeline Behaviour

| Aspect | Initial Sketch | Final Implementation |
|---|---|---|
| Validation | Inline checks in handler / controller | `ValidationBehavior<TRequest, TResponse>` pipeline |
| Why changed | Mixing validation with business logic violates SRP; easy to forget validation in new handlers | Pipeline makes validation automatic for every command, a new handler gets validated by convention |
| Benefit | n/a | Field-level errors with camelCase property names, `CascadeMode.Stop` for clean error messages |

### `DateTime.UtcNow` → `TimeProvider`

| Aspect | Initial Sketch | Final Implementation |
|---|---|---|
| Time access | `DateTime.UtcNow` in validator | `TimeProvider.GetUtcNow()` injected via constructor |
| Why changed | Tests would be fragile near month/year boundaries; couldn't test "January 2025 card expires" deterministically | `FakeTimeProvider` pinned to 2025-06-15 gives complete control |
| Trade-off | Slightly more setup in DI | Eliminates all time-related flakiness |

### Exception-Based Errors → Railway-Oriented `Result<T>`

| Aspect | Initial Sketch | Final Implementation |
|---|---|---|
| Error handling | Throw `ValidationException`, `NotFoundException` | Return `Result<T>` with implicit conversions |
| Why changed | Exceptions are invisible in method signatures and expensive for expected outcomes (declined cards are ~15–30% of real traffic) | `Result<T>` makes failure a first-class, visible part of the return type |
| Trade-off | Must ensure `Match` is called at every boundary | Compiler-safe; eliminates unhandled error states |

### Controller Injection → Startup Extensions

| Aspect | Initial Sketch | Final Implementation |
|---|---|---|
| DI registration | Everything in `Program.cs` | 7 focused extension classes in `Startup/` |
| Why changed | `Program.cs` had massive code bloat; mixing JSON config, MediatR, observability, and Swagger in one file | Each extension has a single responsibility|
| Benefit | n/a | Easier to audit, test, and onboard new developers |

### Non-Nullable Integers → Nullable Fields

| Aspect | Initial Sketch | Final Implementation |
|---|---|---|
| Numeric field types | `int ExpiryMonth`, `long Amount` | `int? ExpiryMonth`, `long? Amount` |
| Why changed | Initially I used non-nullable integers then I noticed that a missing JSON field was silently defaulted to `0` by the model binder. FluentValidation’s `NotEmpty()` never fired because `0` is a valid integer value; there was no way to tell “missing” from “zero”. | Switching to nullable types lets `NotNull()` detect truly absent fields and emit the correct “field is required” error |
| Trade-off | Nullable properties require null-handling in downstream code | Correct required-field validation is non-negotiable for a payment API |

---

## Validation Strategy

### Design Principles

1. **Fail fast, fail clearly:** `CascadeMode.Stop` ensures only the most relevant error fires per field (e.g., if card number is empty, we don't also report "must be 14-19 digits")
2. **Field-level errors:** Each validation failure includes the `field` name (in camelCase) and a human-readable `message`
3. **No bank call on invalid input:** The `ValidationBehavior` short-circuits the pipeline before the handler executes so the bank is never contacted for invalid requests

### Validation Rules

| Field | Rules | Notes |
|---|---|---|
| `cardNumber` | NotEmpty → Length(14,19) → NumericOnly | `CascadeMode.Stop` first failure stops chain |
| `expiryMonth` | InclusiveBetween(1, 12) | Standalone rule |
| `expiryYear` | Must not be in the past | Compared to `TimeProvider.GetUtcNow().Year` |
| `expiry (combined)` | Card must not be expired | Last day of expiry month ≥ today; only runs if month is valid (1–12) |
| `currency` | NotEmpty → Length(3) → SupportedCurrency | Case-insensitive check against whitelist |
| `amount` | GreaterThan(0) | `long` type, minor currency units |
| `cvv` | NotEmpty → Length(3,4) → NumericOnly | `CascadeMode.Stop` |

### Error Response Shape

```json
{
  "code": "VALIDATION_FAILED",
  "message": "One or more validation errors occurred.",
  "errors": [
    { "field": "cardNumber", "message": "Card number must be between 14 and 19 digits." },
    { "field": "expiryMonth", "message": "Expiry month must be between 1 and 12." }
  ]
}
```

---

## Security & PCI-DSS

### Card Data Handling

```
 ┌─────────────────────────────────────────────────────────────────────┐
 │  TRANSIENT (request scope only)         PERSISTED (repository)      │
 │                                                                     │
 │  Full Card Number ──┬── CardNumber[^4..] ──→ Last Four Digits Only  │
 │                     │                                               │
 │                     └── Sent to bank ──────→ Bank Simulator         │
 │                                                                     │
 │  CVV ───────────────── Sent to bank ──────→ Bank Simulator          │
 │                                              CVV Never Stored       │
 │                                                                     │
 ├─────────────────────────────────────────────────────────────────────┤
 │  LOGGED                                                             │
 │                                                                     │
 │   "Card ending 4441"         Full PAN Never Logged                  │
 │   Amount, Currency            CVV Never Logged                      │
 └─────────────────────────────────────────────────────────────────────┘
```

| Principle | Implementation | Verified By |
|---|---|---|
| Only store last four digits | `Payment.GuardCardLastFour` — rejects anything ≠ 4 digits | `Payment_only_stores_last_four_digits` test |
| CVV never persisted | `Payment` has no CVV property | `Payment_entity_does_not_store_cvv` (reflection-based) |
| Full PAN never in response | `ProcessPaymentResponse.FromPayment()` maps `CardNumberLastFour` only | `Response_never_returns_full_pan` test |
| GET response masks card | Domain-level masking — GET handler calls same `FromPayment()` | `Retrieved_payment_masks_card_number` test |
| Full PAN never logged | `BankClient` logs only `cardLastFour` | `Full_pan_never_appears_in_logs` test |
| GET response never has full PAN | `GetPaymentResponse` also uses `FromPayment()` — only last four | `Get_response_never_returns_full_pan` (reflection-based) |

### Structured Logging Safety

All log statements use **structured logging templates** with named placeholders. Card data is explicitly masked before being passed to the logger:

```csharp
// Safe — only last four digits
_logger.LogInformation("Processing payment — card ending {CardLastFour}", cardLastFour);

// Never present in codebase
_logger.LogInformation("Card number: {CardNumber}", request.CardNumber);
```

---

## Error Handling Philosophy

### Two Layers of Defence

1. **`Result<T>` for expected failures** — validation errors, declined cards, bank unavailability
2. **`GlobalExceptionHandler` for unexpected failures** — malformed JSON, framework exceptions, unhandled errors

### HTTP Status Code Mapping

| Scenario | Status Code | Error Code | Rationale |
|---|---|---|---|
| Validation fails | `400 Bad Request` | `VALIDATION_FAILED` | Client sent invalid data; fix and retry |
| Bank rejects request | `400 Bad Request` | `BANK_REJECTED` | Bank-side validation failure; client data issue |
| Payment not found | `404 Not Found` | `PAYMENT_NOT_FOUND` | Standard REST semantics |
| Bank unavailable (503/network/timeout) | `502 Bad Gateway` | `BANK_UNAVAILABLE` | Gateway is working; upstream bank failed |
| Malformed JSON body | `400 Bad Request` | `VALIDATION_FAILED` | `GlobalExceptionHandler` catches `BadHttpRequestException` |
| Unexpected exception | `500 Internal Server Error` | `INTERNAL_ERROR` | Bug in our system |

**Why 502 for bank unavailability (not 503)?**
- `502 Bad Gateway` signals that our gateway is healthy but received an invalid response from the upstream bank
- `503 Service Unavailable` would imply our gateway itself is down, which is misleading
- This follows [RFC 7231 §6.6.3](https://tools.ietf.org/html/rfc7231#section-6.6.3) semantics correctly

---

## Observability

### Correlation & Tracing

| Feature | Implementation |
|---|---|
| Correlation ID | `CorrelationIdMiddleware` — priority: merchant header → `Activity.TraceId` → new GUID |
| Response header | `X-Correlation-Id` echoed on every response |
| W3C trace context | `ActivityTrackingOptions.TraceId \| SpanId` enabled in `LoggerFactoryOptions` |
| Log scope | Every log entry within a request includes `CorrelationId` in its scope |

### Pipeline Logging

The `LoggingPipelineBehavior` wraps every MediatR request:

```
[INF] Handling ProcessPaymentCommand
[INF] Processing payment — card ending 4441, GBP 1050
[INF] Bank response — Authorised for card ending 4441
[INF] Payment abc-123 completed — Authorized, card ending 4441
[INF] Handled ProcessPaymentCommand in 45ms
```

### Exception Handling

```
Client sends malformed JSON
    → Kestrel throws BadHttpRequestException
    → GlobalExceptionHandler catches it
    → Returns clean 400 with { "code": "VALIDATION_FAILED", "message": "The request body is invalid..." }
    → No stack trace exposed to client
```

---

## Testing Strategy

### Testing Pyramid 

```
              ┌─────────────────┐
              │  Integration    │  
              │  (WireMock +    │  Full HTTP pipeline
              │  WebAppFactory) │
              ├─────────────────┤
              │                 │
              │   Unit Tests    │  
              │  (NSubstitute,  │  Handlers, Validators
              │   Shouldly,     │  Domain, Infrastructure
              │   LightBDD)     │
              └─────────────────┘
```

### Test Stack

| Library | Purpose | Why Chosen |
|---|---|---|
| **LightBDD.XUnit2** | BDD test framework | Readable given/when/then step methods; generates living documentation |
| **Shouldly** | Assertion library | `result.ShouldBe(expected)` reads as English; great failure messages |
| **NSubstitute** | Mocking framework | Cleaner syntax than Moq (`Substitute.For<T>()`); no `.Object` dereferencing |
| **WireMock.Net** | HTTP mock server | Real HTTP integration testing; configurable request/response stubs |
| **Bogus** | Fake data generation | `PaymentFaker` generates randomised but valid test data |
| **FakeTimeProvider** | Time control | Pin clock to 2025-06-15 for deterministic expiry validation tests |

### Test Distribution

| Category | What's Tested |
|---|---|
| **Validation : parameterised** (unit) | Every field rule, boundary values, whitespace, case sensitivity  `[Theory]`/`[InlineData]` |
| **Validation : BDD** (unit) | Date-sensitive expiry scenarios requiring a pinned clock (LightBDD) |
| **ProcessPaymentHandler** (unit) | Authorised, declined, bank errors, repo exception, auth code, expiry formatting |
| **GetPaymentHandler** (unit) | Found and not found paths |
| **Domain (Payment)** (unit) | Factory methods, guards, immutability, CVV not stored (reflection), PAN masking |
| **Shared** (unit) | Result pattern, Money value object, SupportedCurrencies whitelist |
| **BankClient** (unit) | All HTTP status codes, network failure, timeout, PAN never logged |
| **Process Payment API** (integration) | End-to-end flows, JSON contract, status casing, PAN masking, malformed JSON, field-specific parse errors |
| **Get Payment API** (integration) | Retrieve, not found, invalid GUID, card masking in response |

### Test Design Decisions
- **Parameterised vs BDD split:** Boundary tests (card number length, amount sign, CVV length) live in `ValidationParameterisedTests` as `[Theory]`/`[InlineData]` this keeps the data together with the assertion and is much more concise than 41 individual BDD scenarios. Date-sensitive expiry tests stay in `ValidationFeature` (LightBDD) because they need a pinned clock and read better as Given/When/Then narratives.
- **Partial class pattern:** Each LightBDD feature has a `Feature.cs` (scenario declarations) and `Feature.Steps.cs` (step implementations) which keeps scenario readability separate from step mechanics
- **Fluent builder:** `PaymentCommandBuilder` creates valid commands with overridable defaults, tests only specify what they're testing
- **No test overlap:** Unit tests mock collaborators; integration tests use real HTTP through `WebApplicationFactory` with WireMock replacing the bank
- **Security tests use reflection:** `Payment_entity_does_not_store_cvv` uses reflection to verify no CVV-related property exists and catches future regressions structurally
- **Time-pinned tests:** `FakeTimeProvider` set to June 15, 2025, all expiry validation tests reference this fixed date

---

## Assumptions

| # | Assumption | Rationale |
|---|---|---|
| 1 | **In-memory storage is acceptable** | The spec provides a skeleton with no database; `ConcurrentDictionary` is sufficient for the exercise while demonstrating the repository abstraction for future swap |
| 2 | **Synchronous payment processing** | The spec describes a request-response flow; no mention of async webhooks or queued processing |
| 3 | **Single acquiring bank** | Only one bank simulator endpoint to integrate with; `BankClient` targets a single `BaseAddress` |
| 4 | **No merchant authentication** | The spec doesn't mention auth; all requests are treated equally |
| 5 | **Three currencies (USD, EUR, GBP)** | The spec says "ensure currency is valid" we chose a realistic whitelist rather than all 180+ ISO codes |
| 6 | **Amount as integer in minor units** | Consistent with industry standard: £10.50 = 1050 pence; avoids floating-point rounding |
| 7 | **Card number is the full PAN** | The spec says 14–19 characters; we validate and forward to bank, but store only last four |
| 8 | **Bank returning 400 is a client data issue** | Mapped to our 400 (not 502) since the bank is explicitly rejecting the payment data |
| 9 | **Payment status `Rejected` means we never called the bank** | Validation failures produce a 400 without any bank interaction, this is the `Rejected` status |
| 10 | **Expiry check uses last day of the month** | A card expiring "01/2025" is valid through 31 January 2025, not just 1 January 2025 |

---

## If I Had More Time

| Improvement | Purpose |
|---|---|
| **Polly resilience policies** | Retry + circuit breaker for transient bank failures (currently we return 502 immediately) |
| **Idempotency keys** | Merchant supplied `Idempotency-Key` header to prevent duplicate charges on retries |
| **Rate limiting** | Per-merchant request throttling to protect the bank integration |
| **Authentication (OAuth2/JWT)** | Merchant identity verification; scope-based access control |
| **Real database (PostgreSQL + EF Core)** | Durable storage; the `IPaymentRepository` interface is ready for this swap |
| **Health check: bank connectivity** | Add a bank ping to the `/health` endpoint; currently checks only that the service is running |
| **API versioning** | Support backward-compatible changes and future enhancements |
| **Availability tests** | Automated checks to ensure uptime and reliability |

### Operational Excellence

| Improvement | Purpose |
|---|---|
| **Structured metric emission** | Payment success rate, latency percentiles (p50/p95/p99), error rates by code |
| **Distributed tracing (OpenTelemetry)** | End-to-end trace linking merchant request → gateway → bank |
| **Azure Application Insights** | Centralised telemetry collection, live metrics, KQL queries, dependency tracking, and failure dashboards out of the box |
| **Alerting (xMatters / PagerDuty)** | Automated on-call alerts on elevated bank failure rates or p99 latency spikes |

### Domain Extensions

| Improvement | Purpose |
|---|---|
| **Refunds & voids** | Extend payment lifecycle beyond initial authorisation |
| **Webhook notifications** | Push payment status changes to merchants asynchronously |
| **Multi-payment-method support** | BNPL, bank transfers, digital wallets |
| **Audit trail / event sourcing** | Compliance-grade change history for every payment state transition |

---

## Project Structure

```
PaymentGateway.Api/
├── Program.cs                              # 22 lines — composes 7 Startup extensions
├── Startup/                                # Service registration & app configuration
│   ├── JsonConfiguration.cs                # camelCase, WhenWritingNull, AllowReadingFromString
│   ├── ObservabilityConfiguration.cs       # W3C TraceId/SpanId, ExceptionHandler, CorrelationId
│   ├── MediatRConfiguration.cs             # Assembly scanning, pipeline order
│   ├── ServiceRegistrationExtensions.cs    # TimeProvider, Repository, BankClient
│   ├── SwaggerConfiguration.cs             # OpenAPI dev-only UI
│   ├── HealthCheckConfiguration.cs         # /health endpoint
│   └── EndpointMappingExtensions.cs        # POST + GET routes, MapError switch
├── Features/
│   ├── ProcessPayment/                     # Vertical slice — payment processing
│   │   ├── ProcessPaymentRequest.cs        # API DTO → ToCommand()
│   │   ├── ProcessPaymentCommand.cs        # MediatR IRequest<Result<T>>
│   │   ├── ProcessPaymentCommandValidator.cs   # FluentValidation + TimeProvider
│   │   ├── ProcessPaymentCommandHandler.cs     # Bank call → Domain → Repository
│   │   └── ProcessPaymentResponse.cs       # FromPayment() factory
│   └── GetPayment/                         # Vertical slice — payment retrieval
│       ├── GetPaymentQuery.cs              # MediatR IRequest<Result<T>>
│       ├── GetPaymentQueryHandler.cs       # Repository lookup → 404 or success
│       └── GetPaymentResponse.cs           # FromPayment() factory
├── Domain/                                 # Aggregate root & value types
│   ├── Payment.cs                          # Sealed, immutable, factory methods
│   └── PaymentStatus.cs                    # Authorized, Declined, Rejected
├── Infrastructure/
│   ├── Bank/                               # Acquiring bank integration
│   │   ├── IBankClient.cs                  # Interface for DI
│   │   ├── BankClient.cs                   # HTTP client with full error handling
│   │   ├── BankPaymentRequest.cs           # snake_case JsonPropertyName
│   │   └── BankPaymentResponse.cs          # snake_case JsonPropertyName
│   ├── Repositories/                       # Data access
│   │   ├── IPaymentRepository.cs           # Add + GetById (ISP)
│   │   └── InMemoryPaymentRepository.cs    # ConcurrentDictionary
│   ├── Middleware/                          # HTTP pipeline middleware
│   │   ├── CorrelationIdMiddleware.cs       # X-Correlation-Id lifecycle
│   │   └── GlobalExceptionHandler.cs       # BadHttpRequestException → 400
│   └── Pipelines/                          # MediatR pipeline behaviours
│       ├── ValidationPipelineBehavior.cs    # FluentValidation → Result.Failure
│       └── LoggingPipelineBehavior.cs       # Elapsed time logging
└── Shared/                                 # Cross-cutting value objects
    ├── Result.cs                           # Railway-oriented Result<T>
    ├── Error.cs                            # Error + ErrorCodes + Errors factory
    ├── ErrorResponse.cs                    # API DTO with ValidationErrorDetail
    ├── Money.cs                            # Amount + Currency value object
    └── SupportedCurrencies.cs              # USD, EUR, GBP whitelist
```

### Test Project Structure

```
PaymentGateway.Api.Tests/           # Unit tests 
├── Features/
│   ├── ValidationParameterisedTests.cs             # parameterised tests (Theory/InlineData)
│   ├── ValidationFeature.cs/.Steps.cs              # date-sensitive BDD scenarios (pinned clock)
│   ├── ProcessPaymentHandlerFeature.cs/.Steps.cs   # scenarios
│   └── GetPaymentHandlerFeature.cs/.Steps.cs       # scenarios
├── Domain/
│   └── PaymentFeature.cs/.Steps.cs                 # scenarios
├── Shared/
│   └── SharedFeature.cs/.Steps.cs                  # scenarios
├── Infrastructure/
│   └── BankClientFeature.cs/.Steps.cs              # scenarios
└── Utilities/
    ├── PaymentCommandBuilder.cs                    # Fluent builder
    └── PaymentFaker.cs                             # Bogus data generator

PaymentGateway.Api.IntegrationTests/    # Integration tests 
├── ProcessPaymentApiFeature.cs/.Steps.cs           # scenarios
├── GetPaymentApiFeature.cs/.Steps.cs               # scenarios
└── Utilities/
    ├── CustomWebApplicationFactory.cs              # WebApplicationFactory + WireMock URL
    ├── WireMockFeatureFixture.cs                   # IAsyncLifetime base class
    └── PaymentFaker.cs                             # Integration test data faker
```

---

## Running the Project

```bash
# Start bank simulator
docker-compose up -d

# Run all 98 tests (79 unit + 19 integration)
dotnet test

# Run the API
dotnet run --project src/PaymentGateway.Api

# Swagger UI (development mode)
# https://localhost:{port}/swagger
```

### Sample Request

```bash
curl -X POST https://localhost:5001/api/payments \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: merchant-txn-001" \
  -d '{
    "cardNumber": "2222405343248877",
    "expiryMonth": 4,
    "expiryYear": 2030,
    "currency": "GBP",
    "amount": 100,
    "cvv": "123"
  }'
```

### Sample Response (200 OK)

```json
{
  "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "Authorized",
  "cardNumberLastFour": "8877",
  "expiryMonth": 4,
  "expiryYear": 2030,
  "currency": "GBP",
  "amount": 100
}
```

---

*tests include (unit + integration) • .NET 8.0 • LightBDD + Shouldly + NSubstitute + WireMock*
