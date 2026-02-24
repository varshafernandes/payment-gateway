using LightBDD.XUnit2;

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
}
