namespace PaymentGateway.Api.Startup;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddHealthCheckConfiguration(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        return app;
    }
}
