using PaymentGateway.Api.Infrastructure.Bank;
using PaymentGateway.Api.Infrastructure.Repositories;

namespace PaymentGateway.Api.Startup;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddPaymentRepository();
        services.AddBankClient(configuration);

        return services;
    }

    private static void AddPaymentRepository(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
    }

    private static void AddBankClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IBankClient, BankClient>(client =>
        {
            var bankUrl = configuration.GetValue<string>("BankSimulator:Url")
                ?? "http://localhost:8080";
            client.BaseAddress = new Uri(bankUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }
}
