using System.Net;
using System.Net.Http.Json;

using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Features.GetPayment;
using PaymentGateway.Api.Features.ProcessPayment;
using PaymentGateway.Api.IntegrationTests.Utilities;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PaymentGateway.Api.IntegrationTests;

public partial class GetPaymentApiFeature : WireMockFeatureFixture
{
    private readonly PaymentFaker _faker = new();
    private ProcessPaymentRequest _request = null!;
    private HttpResponseMessage _httpResponse = null!;
    private ProcessPaymentResponse? _createdPayment;
    private GetPaymentResponse? _retrievedPayment;

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

    private async Task Given_a_payment_has_been_created()
    {
        _request = _faker.ValidRequest();
        var response = await Client.PostAsJsonAsync("/api/payments", _request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _createdPayment = await response.Content.ReadFromJsonAsync<ProcessPaymentResponse>();
        _createdPayment.ShouldNotBeNull();
    }

    private async Task When_the_payment_is_retrieved_by_id()
    {
        _httpResponse = await Client.GetAsync($"/api/payments/{_createdPayment!.Id}");
        if (_httpResponse.IsSuccessStatusCode)
            _retrievedPayment = await _httpResponse.Content.ReadFromJsonAsync<GetPaymentResponse>();
    }

    private async Task When_a_non_existent_payment_is_requested()
    {
        _httpResponse = await Client.GetAsync($"/api/payments/{Guid.NewGuid()}");
    }

    private async Task When_an_invalid_guid_is_requested()
    {
        _httpResponse = await Client.GetAsync("/api/payments/not-a-guid");
    }

    private Task Then_the_response_status_code_is_200()
    {
        _httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        return Task.CompletedTask;
    }

    private Task Then_the_response_status_code_is_404()
    {
        _httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        return Task.CompletedTask;
    }

    private Task Then_the_retrieved_payment_matches_the_original()
    {
        _retrievedPayment.ShouldNotBeNull();
        _retrievedPayment!.Id.ShouldBe(_createdPayment!.Id);
        _retrievedPayment.Status.ShouldBe(PaymentStatus.Authorized.ToString());
        _retrievedPayment.CardNumberLastFour.ShouldBe(_request.CardNumber[^4..]);
        _retrievedPayment.ExpiryMonth.ShouldBe(_request.ExpiryMonth);
        _retrievedPayment.ExpiryYear.ShouldBe(_request.ExpiryYear);
        _retrievedPayment.Currency.ShouldBe(_request.Currency);
        _retrievedPayment.Amount.ShouldBe(_request.Amount);
        return Task.CompletedTask;
    }

    private async Task Then_the_get_response_body_does_not_contain_full_card_number()
    {
        var body = await _httpResponse.Content.ReadAsStringAsync();
        body.ShouldNotContain(_request.CardNumber);
    }

    private Task Then_the_get_response_contains_only_last_four_digits()
    {
        _retrievedPayment.ShouldNotBeNull();
        _retrievedPayment!.CardNumberLastFour.Length.ShouldBe(4);
        _retrievedPayment.CardNumberLastFour.ShouldBe(_request.CardNumber[^4..]);
        return Task.CompletedTask;
    }
}
