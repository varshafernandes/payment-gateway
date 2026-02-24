using Microsoft.OpenApi.Models;

namespace PaymentGateway.Api.Startup;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Payment Gateway API",
                Version = "v1",
                Description = "API for merchants to process and retrieve card payments."
            });
        });

        return services;
    }

    public static WebApplication UseSwaggerIfDevelopment(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.UseSwagger();
        app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Gateway v1"));

        return app;
    }
}
