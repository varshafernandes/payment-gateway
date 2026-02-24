using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Infrastructure.Bank;

using WireMock.Server;

namespace PaymentGateway.Api.IntegrationTests.Utilities;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public WireMockServer BankSimulator { get; }

    public CustomWebApplicationFactory()
    {
        BankSimulator = WireMockServer.Start();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient<IBankClient, BankClient>(client =>
            {
                client.BaseAddress = new Uri(BankSimulator.Url!);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BankSimulator.Stop();
            BankSimulator.Dispose();
        }

        base.Dispose(disposing);
    }
}
