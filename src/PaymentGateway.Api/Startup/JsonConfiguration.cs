using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Startup;

public static class JsonConfiguration
{
    public static IServiceCollection AddJsonConfiguration(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        });

        return services;
    }
}
