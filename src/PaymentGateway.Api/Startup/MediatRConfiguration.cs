using FluentValidation;

using MediatR;

using PaymentGateway.Api.Infrastructure.Pipelines;

namespace PaymentGateway.Api.Startup;

public static class MediatRConfiguration
{
    public static IServiceCollection AddMediatRConfiguration(this IServiceCollection services)
    {
        services.AddMediatR(typeof(Program).Assembly);
        services.AddValidatorsFromAssemblyContaining(typeof(Program));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
