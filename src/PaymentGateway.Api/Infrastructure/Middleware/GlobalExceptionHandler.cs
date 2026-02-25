using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Diagnostics;

using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Infrastructure.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    private static readonly Dictionary<string, (string Field, string LeadingZeroMessage, string TypeMessage)> FieldHints = new()
    {
        ["expiryMonth"] = ("ExpiryMonth", "Expiry month must be between 1 and 12 — no leading zeros (e.g. use 1, not 01).", "Expiry month must be a whole number between 1 and 12."),
        ["expiryYear"]  = ("ExpiryYear",  "Expiry year must be a 4-digit year — no leading zeros.", "Expiry year must be a whole number (e.g. 2030)."),
        ["amount"]      = ("Amount",      "Amount must be a whole number in minor units (e.g. 1050 for £10.50) — no leading zeros.", "Amount must be a whole number in minor units (e.g. 1050 for £10.50) — decimal values are not accepted."),
    };

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

        var errors = TryExtractJsonErrors(exception.InnerException as JsonException)
                     ?? [new ValidationErrorDetail("body", "The request body is invalid. Check that all fields are correctly formatted.")];

        var response = new ErrorResponse(
            ErrorCodes.ValidationFailed,
            "One or more fields in the request are invalid.",
            errors);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static IReadOnlyList<ValidationErrorDetail>? TryExtractJsonErrors(JsonException? jsonEx)
    {
        if (jsonEx is null) return null;

        var message = jsonEx.Message;

        var pathMatch = Regex.Match(message, @"\$\.(\w+)");
        var jsonField = pathMatch.Success ? pathMatch.Groups[1].Value : null;

        if (jsonField is not null && FieldHints.TryGetValue(jsonField, out var hint))
        {
            var friendlyMessage = message.Contains("leading zero", StringComparison.OrdinalIgnoreCase)
                ? hint.LeadingZeroMessage
                : hint.TypeMessage;

            return [new ValidationErrorDetail(hint.Field, friendlyMessage)];
        }

        if (jsonField is not null)
        {
            return [new ValidationErrorDetail(jsonField, "The value provided is not valid for this field.")];
        }

        return [new ValidationErrorDetail("body", "The request body is not valid JSON. Check for missing commas, brackets, or incorrectly formatted fields.")];
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
