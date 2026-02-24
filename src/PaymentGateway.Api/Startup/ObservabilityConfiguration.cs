using PaymentGateway.Api.Infrastructure.Middleware;

namespace PaymentGateway.Api.Startup;

public static class ObservabilityConfiguration
{
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.Configure<LoggerFactoryOptions>(options =>
        {
            options.ActivityTrackingOptions =
                ActivityTrackingOptions.TraceId |
                ActivityTrackingOptions.SpanId;
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }
}
