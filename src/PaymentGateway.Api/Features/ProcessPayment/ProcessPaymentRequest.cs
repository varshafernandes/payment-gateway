namespace PaymentGateway.Api.Features.ProcessPayment;

public sealed record ProcessPaymentRequest(
    string? CardNumber,
    int? ExpiryMonth,
    int? ExpiryYear,
    string? Currency,
    long? Amount,
    string? Cvv)
{
    public ProcessPaymentCommand ToCommand() =>
        new(CardNumber, ExpiryMonth, ExpiryYear, Currency, Amount, Cvv);
}
