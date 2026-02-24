using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Shared;

public sealed record ErrorResponse(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<ValidationErrorDetail>? Errors = null)
{
    public static ErrorResponse FromError(Error error)
    {
        IReadOnlyList<ValidationErrorDetail>? details = error.ValidationErrors.Count > 0
            ? error.ValidationErrors
                .Select(v => new ValidationErrorDetail(v.PropertyName, v.Message))
                .ToList()
            : null;

        return new ErrorResponse(error.Code, error.Message, details);
    }
}

public sealed record ValidationErrorDetail(string Field, string Message);
