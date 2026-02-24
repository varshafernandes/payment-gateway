using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Features.ProcessPayment;

public sealed record ProcessPaymentResponse(
    Guid Id,
    string Status,
    string CardNumberLastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    long Amount)
{
    public static ProcessPaymentResponse FromPayment(Payment payment) =>
        new(
            payment.Id,
            payment.Status.ToString(),
            payment.CardNumberLastFour,
            payment.ExpiryMonth,
            payment.ExpiryYear,
            payment.Amount.Currency,
            payment.Amount.Amount);
}

