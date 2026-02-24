namespace PaymentGateway.Api.Shared;

public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
        ValidationErrors = Array.Empty<ValidationError>();
    }

    public Error(string code, string message, IReadOnlyList<ValidationError> validationErrors)
    {
        Code = code;
        Message = message;
        ValidationErrors = validationErrors;
    }
}

public static class Errors
{
    public static Error Validation(string message) =>
        new(ErrorCodes.ValidationFailed, message);

    public static Error Validation(string message, IReadOnlyList<ValidationError> validationErrors) =>
        new(ErrorCodes.ValidationFailed, message, validationErrors);

    public static Error PaymentNotFound(Guid paymentId) =>
        new(ErrorCodes.PaymentNotFound, $"Payment with id '{paymentId}' was not found.");

    public static Error BankUnavailable() =>
        new(ErrorCodes.BankUnavailable, "Unable to process payment at this time. Please retry.");

    public static Error BankRejected(string reason) =>
        new(ErrorCodes.BankRejected, reason);

    public static Error Internal(string message) =>
        new(ErrorCodes.InternalError, message);
}

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string PaymentNotFound = "PAYMENT_NOT_FOUND";
    public const string BankUnavailable = "BANK_UNAVAILABLE";
    public const string BankRejected = "BANK_REJECTED";
    public const string InternalError = "INTERNAL_ERROR";
}
