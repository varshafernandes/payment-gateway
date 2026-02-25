using System.Net;
using System.Net.Http.Json;
using System.Text;

using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Features.GetPayment;
using PaymentGateway.Api.Features.ProcessPayment;
using PaymentGateway.Api.IntegrationTests.Utilities;
using PaymentGateway.Api.Shared;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PaymentGateway.Api.IntegrationTests;

public partial class ProcessPaymentApiFeature : WireMockFeatureFixture
{
    private readonly PaymentFaker _faker = new();
    private ProcessPaymentRequest _request = null!;
    private HttpResponseMessage _httpResponse = null!;
    private ProcessPaymentResponse? _paymentResponse;

    private Task Given_the_bank_will_authorise()
    {
        BankSimulator.Reset();
        BankSimulator
            .Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { authorized = true, authorization_code = "test-auth-code" }));
        return Task.CompletedTask;
    }

    private Task Given_the_bank_will_decline()
    {
        BankSimulator.Reset();
        BankSimulator
            .Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { authorized = false, authorization_code = "" }));
        return Task.CompletedTask;
    }

    private Task Given_the_bank_will_return_503()
    {
        BankSimulator.Reset();
        BankSimulator
            .Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(503));
        return Task.CompletedTask;
    }

    private Task Given_the_bank_will_return_400()
    {
        BankSimulator.Reset();
        BankSimulator
            .Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(400));
        return Task.CompletedTask;
    }

    private async Task When_a_valid_payment_request_is_posted()
    {
        _request = _faker.ValidRequest();
        _httpResponse = await Client.PostAsJsonAsync("/api/payments", _request);
        if (_httpResponse.IsSuccessStatusCode)
            _paymentResponse = await _httpResponse.Content.ReadFromJsonAsync<ProcessPaymentResponse>();
    }

    private async Task When_an_invalid_payment_request_is_posted()
    {
        var invalidRequest = new ProcessPaymentRequest("", 0, 0, "", 0, "");
        _httpResponse = await Client.PostAsJsonAsync("/api/payments", invalidRequest);
    }

    private async Task When_the_health_endpoint_is_called()
    {
        _httpResponse = await Client.GetAsync("/health");
    }

    private async Task When_a_malformed_json_body_is_posted()
    {
        const string malformedJson = """
            {
                "cardNumber": "2222405343248877",
                "expiryMonth": 09,
                "expiryYear": 2030,
                "currency": "GBP",
                "amount": 100,
                "cvv": "123"
            }
            """;

        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");
        _httpResponse = await Client.PostAsync("/api/payments", content);
    }

    private async Task When_a_syntax_broken_json_body_is_posted()
    {
        const string brokenJson = """
            {
                "cardNumber": "2222405343248877"
                "expiryMonth": 1,
                "expiryYear": 2030,
                "currency": "GBP",
                "amount": 100,
                "cvv": "123"
            }
            """;

        var content = new StringContent(brokenJson, Encoding.UTF8, "application/json");
        _httpResponse = await Client.PostAsync("/api/payments", content);
    }

    private async Task When_expiry_month_with_leading_zero_is_posted()
    {
        const string json = """
            {
                "cardNumber": "2222405343248877",
                "expiryMonth": 01,
                "expiryYear": 2030,
                "currency": "GBP",
                "amount": 100,
                "cvv": "123"
            }
            """;

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _httpResponse = await Client.PostAsync("/api/payments", content);
    }

    private async Task When_a_decimal_amount_is_posted()
    {
        const string json = """
            {
                "cardNumber": "2222405343248877",
                "expiryMonth": 1,
                "expiryYear": 2030,
                "currency": "GBP",
                "amount": 10.50,
                "cvv": "123"
            }
            """;

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _httpResponse = await Client.PostAsync("/api/payments", content);
    }

    private async Task When_required_numeric_fields_are_missing()
    {
        const string json = """
            {
                "cardNumber": "2222405343248877",
                "currency": "GBP",
                "cvv": "123"
            }
            """;

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _httpResponse = await Client.PostAsync("/api/payments", content);
    }

    private Task Then_the_response_status_code_is_200()
    {
        _httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        return Task.CompletedTask;
    }

    private Task Then_the_response_status_code_is_400()
    {
        _httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        return Task.CompletedTask;
    }

    private Task Then_the_response_status_code_is_502()
    {
        _httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        return Task.CompletedTask;
    }

    private Task Then_the_response_status_is_Authorized()
    {
        _paymentResponse.ShouldNotBeNull();
        _paymentResponse!.Status.ShouldBe(PaymentStatus.Authorized.ToString());
        return Task.CompletedTask;
    }

    private Task Then_the_response_status_is_Declined()
    {
        _paymentResponse.ShouldNotBeNull();
        _paymentResponse!.Status.ShouldBe(PaymentStatus.Declined.ToString());
        return Task.CompletedTask;
    }

    private Task Then_the_response_contains_the_correct_card_details()
    {
        _paymentResponse.ShouldNotBeNull();
        _paymentResponse!.CardNumberLastFour.ShouldBe(_request.CardNumber[^4..]);
        _paymentResponse.ExpiryMonth.ShouldBe(_request.ExpiryMonth);
        _paymentResponse.ExpiryYear.ShouldBe(_request.ExpiryYear);
        _paymentResponse.Currency.ShouldBe(_request.Currency);
        _paymentResponse.Amount.ShouldBe(_request.Amount);
        return Task.CompletedTask;
    }

    private async Task Then_the_payment_can_be_retrieved_by_id()
    {
        _paymentResponse.ShouldNotBeNull();
        var getResponse = await Client.GetAsync($"/api/payments/{_paymentResponse!.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var retrievedPayment = await getResponse.Content.ReadFromJsonAsync<GetPaymentResponse>();
        retrievedPayment.ShouldNotBeNull();
        retrievedPayment!.Id.ShouldBe(_paymentResponse.Id);
        retrievedPayment.Status.ShouldBe(PaymentStatus.Authorized.ToString());
        retrievedPayment.CardNumberLastFour.ShouldBe(_request.CardNumber[^4..]);
    }

    private async Task Then_the_response_body_contains_validation_error()
    {
        var errorResponse = await _httpResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull();
        errorResponse!.Code.ShouldBe("VALIDATION_FAILED");
        errorResponse.Message.ShouldNotBeNullOrWhiteSpace();
    }

    private async Task Then_the_error_identifies_ExpiryMonth_with_leading_zero_message()
    {
        var errorResponse = await _httpResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull();
        errorResponse!.Code.ShouldBe("VALIDATION_FAILED");
        errorResponse.Errors.ShouldNotBeNull();
        errorResponse.Errors!.ShouldContain(e =>
            e.Field == "ExpiryMonth" && e.Message.Contains("leading zero"),
            "Expected a field-specific error on ExpiryMonth mentioning leading zeros.");
    }

    private async Task Then_the_error_identifies_Amount_with_decimal_message()
    {
        var errorResponse = await _httpResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull();
        errorResponse!.Code.ShouldBe("VALIDATION_FAILED");
        errorResponse.Errors.ShouldNotBeNull();
        errorResponse.Errors!.ShouldContain(e =>
            e.Field == "Amount" && e.Message.Contains("minor units"),
            "Expected a field-specific error on Amount mentioning minor units / no decimals.");
    }

    private async Task Then_the_error_says_request_body_is_not_valid_json()
    {
        var errorResponse = await _httpResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull();
        errorResponse!.Code.ShouldBe("VALIDATION_FAILED");
        errorResponse.Errors.ShouldNotBeNull();
        errorResponse.Errors!.ShouldContain(e =>
            e.Field == "body" && e.Message.Contains("not valid JSON"),
            "Expected a syntax error message mentioning 'not valid JSON'.");
    }

    private async Task Then_the_response_has_required_field_errors_for_missing_numeric_fields()
    {
        var errorResponse = await _httpResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull();
        errorResponse!.Code.ShouldBe("VALIDATION_FAILED");
        errorResponse.Errors.ShouldNotBeNull();
        errorResponse.Errors!.ShouldContain(e => e.Field == "expiryMonth", "Expected required error for expiryMonth.");
        errorResponse.Errors!.ShouldContain(e => e.Field == "expiryYear", "Expected required error for expiryYear.");
        errorResponse.Errors!.ShouldContain(e => e.Field == "amount", "Expected required error for amount.");
    }

    private async Task Then_the_response_contains_structured_validation_errors()
    {
        var errorResponse = await _httpResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull();
        errorResponse!.Code.ShouldBe("VALIDATION_FAILED");
        errorResponse.Message.ShouldBe("One or more validation errors occurred.");
        errorResponse.Errors.ShouldNotBeNull();
        errorResponse.Errors!.Count.ShouldBeGreaterThan(0);

        foreach (var error in errorResponse.Errors)
        {
            error.Field.ShouldNotBeNullOrWhiteSpace();
            error.Message.ShouldNotBeNullOrWhiteSpace();
        }
    }

    private async Task Then_the_response_body_contains_bank_rejected_error()
    {
        var errorResponse = await _httpResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull();
        errorResponse!.Code.ShouldBe("BANK_REJECTED");
    }

    private Task Then_the_bank_was_not_called()
    {
        var bankRequests = BankSimulator.LogEntries;
        bankRequests.ShouldBeEmpty("Bank should not be called when validation fails.");
        return Task.CompletedTask;
    }

    private async Task Then_the_response_body_does_not_contain_full_card_number()
    {
        var body = await _httpResponse.Content.ReadAsStringAsync();
        body.ShouldNotContain(_request.CardNumber);
        body.ShouldContain(_request.CardNumber[^4..]);
    }

    private async Task Then_the_json_uses_camelCase_property_names()
    {
        var body = await _httpResponse.Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("id", out _).ShouldBeTrue("Missing 'id' property");
        root.TryGetProperty("status", out _).ShouldBeTrue("Missing 'status' property");
        root.TryGetProperty("cardNumberLastFour", out _).ShouldBeTrue("Missing 'cardNumberLastFour' property");
        root.TryGetProperty("expiryMonth", out _).ShouldBeTrue("Missing 'expiryMonth' property");
        root.TryGetProperty("expiryYear", out _).ShouldBeTrue("Missing 'expiryYear' property");
        root.TryGetProperty("currency", out _).ShouldBeTrue("Missing 'currency' property");
        root.TryGetProperty("amount", out _).ShouldBeTrue("Missing 'amount' property");

        root.TryGetProperty("Id", out _).ShouldBeFalse("Found PascalCase 'Id' — expected camelCase");
        root.TryGetProperty("Status", out _).ShouldBeFalse("Found PascalCase 'Status'");
        root.TryGetProperty("CardNumberLastFour", out _).ShouldBeFalse("Found PascalCase 'CardNumberLastFour'");
        root.TryGetProperty("ExpiryMonth", out _).ShouldBeFalse("Found PascalCase 'ExpiryMonth'");
        root.TryGetProperty("ExpiryYear", out _).ShouldBeFalse("Found PascalCase 'ExpiryYear'");
        root.TryGetProperty("Currency", out _).ShouldBeFalse("Found PascalCase 'Currency'");
        root.TryGetProperty("Amount", out _).ShouldBeFalse("Found PascalCase 'Amount'");
    }

    private async Task Then_the_json_does_not_expose_internal_fields()
    {
        var body = await _httpResponse.Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("cardNumber", out _).ShouldBeFalse("Found 'cardNumber' — full PAN must not be exposed");
        root.TryGetProperty("CardNumber", out _).ShouldBeFalse("Found 'CardNumber' — full PAN must not be exposed");
        root.TryGetProperty("cvv", out _).ShouldBeFalse("Found 'cvv' — CVV must not be exposed");
        root.TryGetProperty("Cvv", out _).ShouldBeFalse("Found 'Cvv' — CVV must not be exposed");
        root.TryGetProperty("authorizationCode", out _).ShouldBeFalse("Found 'authorizationCode' — internal field");
        root.TryGetProperty("AuthorizationCode", out _).ShouldBeFalse("Found 'AuthorizationCode' — internal field");
    }

    private async Task Then_the_raw_status_value_is_exactly_Authorized()
    {
        var body = await _httpResponse.Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var status = doc.RootElement.GetProperty("status").GetString();

        status.ShouldBe("Authorized");
    }
}
