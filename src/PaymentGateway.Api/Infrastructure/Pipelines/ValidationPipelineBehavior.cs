using FluentValidation;
using MediatR;

using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Infrastructure.Pipelines;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var validationErrors = failures
            .Select(f => new ValidationError(ToCamelCase(f.PropertyName), f.ErrorMessage))
            .Distinct()
            .ToList();

        var summary = string.Join("; ", validationErrors.Select(e => e.Message));

        _logger.LogWarning(
            "Validation failed for {RequestType}: {Errors}",
            typeof(TRequest).Name,
            summary);

        var error = Errors.Validation("One or more validation errors occurred.", validationErrors);
        return ResultFactory.CreateFailure<TResponse>(error);
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}

