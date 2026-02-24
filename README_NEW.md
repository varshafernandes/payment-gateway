# Payment Gateway API

A payment processing API that allows merchants to process card payments and retrieve payment details.

## Quick Demo (3 Minutes)

You can follow these steps to see the gateway in action.

```bash
# 1. Start bank simulator
docker-compose up -d

# 2. Run the API
dotnet run --project src/PaymentGateway.Api

# 3. Open Swagger UI (check console for actual port)
#    https://localhost:7092/swagger
```

| Step | What to do | Expected result |
|------|-----------|----------------|
| **1** | POST `/api/payments` with card ending in **1** (odd) | `200 OK` — status: `"Authorized"` |
| **2** | POST `/api/payments` with card ending in **8** (even) | `200 OK` — status: `"Declined"` |
| **3** | POST `/api/payments` with card ending in **0** | `502 Bad Gateway` (bank returned 503) |
| **4** | POST `/api/payments` with invalid data (e.g. expired card) | `400 Bad Request` — validation errors |
| **5** | Copy the `id` from step 1, GET `/api/payments/{id}` | `200 OK` — same payment, card masked to last four |
| **6** | GET `/api/payments/{random-guid}` | `404 Not Found` |

Example request body (card ends in 1 → Authorized):
```json
{
  "cardNumber": "1234156819781631",
  "expiryMonth": 6,
  "expiryYear": 2040,
  "currency": "GBP",
  "amount": 1256,
  "cvv": "1234"
}
```

### Running Tests

```bash
# Run all 80 tests (65 unit + 15 integration)
dotnet test

# Run with test coverage
dotnet test /p:CollectCoverage=true
```

## API Endpoints

### Process Payment

**POST** `/api/payments`

Request:
```json
{
  "cardNumber": "1111222233334441",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "currency": "GBP",
  "amount": 1050,
  "cvv": "123"
}
```

Response (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Authorized",
  "cardNumberLastFour": "4441",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "amount": 1050,
  "currency": "GBP"
}
```

### Get Payment

**GET** `/api/payments/{id}`

Response (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Authorized",
  "cardNumberLastFour": "4441",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "amount": 1050,
  "currency": "GBP"
}
```

## HTTP Status Codes

| Scenario | Our Gateway Status | Response Body |
|----------|-------------------|---------------|
| Authorized payment | `200 OK` | Payment with `status: "Authorized"` |
| Declined payment | `200 OK` | Payment with `status: "Declined"` |
| Validation failure | `400 Bad Request` | Error with field-level details |
| Bank unavailable | `502 Bad Gateway` | Empty (upstream bank failed) |
| Payment not found | `404 Not Found` | Error with `PAYMENT_NOT_FOUND` code |

## Bank Simulator → Gateway Status Translation

The gateway sits between the merchant and the bank. The bank simulator returns its own status codes, and our gateway **translates** them into merchant-friendly responses:

```
Merchant  →  Our Gateway (port 7092)  →  Bank Simulator (port 8080)
```

| Card ending | Bank simulator returns | Our gateway returns to merchant |
|-------------|----------------------|--------------------------------|
| Odd (1,3,5,7,9) | `200 OK` with `{ "authorized": true, "authorization_code": "..." }` | `200 OK` with `status: "Authorized"` |
| Even (2,4,6,8) | `200 OK` with `{ "authorized": false, "authorization_code": "" }` | `200 OK` with `status: "Declined"` |
| Zero (0) | `503 Service Unavailable` | `502 Bad Gateway` |
| Missing fields | `400 Bad Request` | `400 Bad Request` with `BANK_REJECTED` |

**Key points:**
- Both authorized and declined come from the bank as HTTP **200** ,the `authorized` boolean determines the outcome, not the HTTP status
- The bank's raw fields (`authorized`, `authorization_code`, `expiry_date`) are internal plumbing, merchants see our clean `status` field instead
- Our API accepts `expiryMonth` + `expiryYear` separately; the handler combines them into `expiry_date: "MM/YYYY"` before sending to the bank
- `authorization_code` is stored internally but not exposed in the merchant response (available for reconciliation)

## Payment Status

- **Authorized**: Bank approved the payment → payment is stored, response returned
- **Declined**: Bank declined the payment → payment is stored with declined status
- **Rejected**: Request failed our validation → bank was **never called**, `400` returned immediately

## Architecture

See [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) for comprehensive design decisions, testing strategy, and future roadmap.

## Key Features

 **Validation**: FluentValidation with clear error messages  
 **PCI-DSS Compliance**: Only stores last 4 card digits  
 **Error Handling**: Centralized error codes and HTTP mapping  
 **Testing**: Unit, integration tests  
 **Logging**: Structured logging  
 **Documentation**: OpenAPI/Swagger with examples  

## Project Structure

```
src/PaymentGateway.Api/
├── Features/
│   ├── ProcessPayment/  # Payment processing feature
│   └── GetPayment/      # Payment retrieval feature
├── Domain/              # Domain models (Payment aggregate)
├── Infrastructure/      # External integrations (Bank, Repository)
├── Shared/              # Cross-cutting concerns (Result, Error, Money)
└── Controllers/         # HTTP endpoints

test/PaymentGateway.Api.Tests/
├── Features/            # Feature handler tests
├── Domain/              # Domain model tests
├── Shared/              # Shared concern tests
└── Integration/         # API integration tests
```

## Development

### Making Changes

1. Pick a feature folder (ProcessPayment, GetPayment, etc.)
2. Modify feature files (Request, Validator, Handler, Response)
3. Update/add tests for the feature
4. Run tests: `dotnet test`
5. Verify with manual API test or Swagger UI

### Adding New Validation Rule

1. Edit `Features/ProcessPayment/ProcessPaymentValidator.cs`
2. Add new `RuleFor()` with error code
3. Add test in `ProcessPaymentValidatorTests.cs`
4. Run tests to verify

### Adding New Currency Support

1. Update `AllowedCurrencies` set in `Shared/Money.cs`
2. Update array in `ProcessPaymentValidator.cs`
3. Add unit test to `MoneyTests.cs`

## Configuration

Edit `src/PaymentGateway.Api/appsettings.json`:

```json
{
  "BankSimulator": {
    "Url": "http://localhost:8080"  // Point to real bank in production
  }
}
```

## Security

### PCI-DSS Compliance 

- Card numbers: Only last 4 digits stored
- CVV: Never stored, only used during validation
- HTTPS: Enforced in production
- Structured logging: Sensitive data never logged

### Sensitive Data

Never log or expose:
- Full card numbers
- CVV/CVC
- Expiry dates in full (only month/year OK)
- Authorization codes in logs

## Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 80
CMD ["dotnet", "PaymentGateway.Api.dll"]
```

Build and run:
```bash
docker build -t payment-gateway:latest .
docker run -p 8080:80 payment-gateway:latest
```

### Environment

| Environment | Bank Url | Logging |
|-------------|----------|---------|
| Local | http://localhost:8080 | Debug |

## Testing Strategy

### Testing Pyramid

```
            /  Integration  \     15 tests — Full HTTP pipeline
           /   (WireMock +   \    Real HTTP through WebApplicationFactory
          /   WebAppFactory)  \   Bank simulator replaced by WireMock
         /                     \
        /     Unit Tests        \  65 tests — Handlers, Validators,
       /   (NSubstitute,         \ Domain, Infrastructure
      /    Shouldly, LightBDD)    \
     /____________________________ \
```

### What's Tested

| Layer | Count | What |
|-------|-------|------|
| Validation (unit) | 31 | Every field rule, boundary values, edge cases |
| Process Payment Handler (unit) | 9 | Authorized, declined, bank errors, auth code |
| Get Payment Handler (unit) | 2 | Found vs not-found paths |
| Domain / Payment (unit) | 8 | Factory methods, guards, immutability, CVV not stored |
| Shared / Result / Money (unit) | 6 | Result pattern, Money value object, currencies |
| BankClient (unit) | 7 | All HTTP status codes, network failure, timeout |
| Process Payment API (integration) | 11 | End-to-end via HTTP, JSON contract, status casing |
| Get Payment API (integration) | 4 | Retrieve, not found, invalid GUID |

### Key Testing Decisions

- **LightBDD** gives BDD-style given/when/then with living documentation
- **No test overlap**: Unit tests mock collaborators; integration tests use real HTTP
- **FakeTimeProvider**: Clock pinned to June 15, 2025 for deterministic expiry tests
- **Security tests use reflection**: Verify CVV is never stored at the domain level
- **Fluent builder**: `PaymentCommandBuilder` — tests only specify what they're testing

## Dependencies

- FluentValidation.AspNetCore
- MediatR
- LightBDD.XUnit2 (testing)
- Shouldly (testing)
- NSubstitute (testing)
- WireMock.Net (testing)
- Bogus (testing)

## Known Limitations

1. **In-memory storage**: Data lost on restart (add a database in the future)
2. **Single bank**: Only one acquiring bank supported (support for multi payment provider)
3. **Synchronous only**: No async workflows (add webhooks for async notifications)
4. **No authentication**: All merchants treated the same (add OAuth2)
5. **Fixed currencies**: Limited to 3 currencies — USD, EUR, GBP (expand to support more currencies)

## Performance

- **Payment processing**: Latency dominated by bank round-trip; gateway adds minimal overhead
- **Throughput**: Designed to be lightweight and non-blocking; in-memory storage allows quick access
- **Validation**: FluentValidation with short-circuit (`CascadeMode.Stop`)
- **Storage**: Retrieval via `ConcurrentDictionary`, payments are stored in a way that’s thread safe for multiple users at once.

## Support & Troubleshooting

### Bank simulator not responding (503)

1. Check if Docker container is running: `docker ps`
2. Start simulator: `docker-compose up -d`
3. Verify URL: `curl http://localhost:8080/health` (if health endpoint exists)

### Validation errors

- All error codes are in `Shared/Error.cs`
- Use Swagger UI to see field validation rules
- Check `ProcessPaymentValidator.cs` for exact rules

### Tests failing

1. Run tests in isolation: `dotnet test --filter "ClassName=XTests"`
2. Check test logs: `dotnet test --logger="console;verbosity=detailed"`
3. Verify bank simulator is running for integration tests

## Contributing

1. Follow existing code style (PascalCase, async/await)
2. Write tests for all features
3. Update ARCHITECTURE.md if making significant changes
4. Run `dotnet format` before committing
