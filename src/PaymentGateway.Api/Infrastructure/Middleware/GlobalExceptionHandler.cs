using Microsoft.AspNetCore.Diagnostics;

using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Infrastructure.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return exception switch
        {
            BadHttpRequestException badRequest => await HandleBadRequestAsync(httpContext, badRequest, cancellationToken),
            _ => await HandleUnexpectedAsync(httpContext, exception, cancellationToken)
        };
    }

    private async Task<bool> HandleBadRequestAsync(
        HttpContext httpContext,
        BadHttpRequestException exception,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(exception, "Bad request: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/json";

        var response = new ErrorResponse(
            ErrorCodes.ValidationFailed,
            "The request body is invalid. Check that all fields are correctly formatted.",
            null);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private async Task<bool> HandleUnexpectedAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var response = new ErrorResponse(
            ErrorCodes.InternalError,
            "An unexpected error occurred.",
            null);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
