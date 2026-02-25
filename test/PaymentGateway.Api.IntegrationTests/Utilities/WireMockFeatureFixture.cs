using System.Net;

using LightBDD.XUnit2;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PaymentGateway.Api.IntegrationTests.Utilities;

public abstract class WireMockFeatureFixture : FeatureFixture, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();

    protected HttpClient Client { get; private set; } = null!;

    protected WireMockServer BankSimulator => _factory.BankSimulator;

    public Task InitializeAsync()
    {
        Client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    protected Task SetupBankWillAuthorise()
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

    protected Task AssertStatusCode(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.ShouldBe(expected);
        return Task.CompletedTask;
    }
}
