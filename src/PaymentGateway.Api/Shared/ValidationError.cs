namespace PaymentGateway.Api.Shared;

public sealed record ValidationError(string PropertyName, string Message);
