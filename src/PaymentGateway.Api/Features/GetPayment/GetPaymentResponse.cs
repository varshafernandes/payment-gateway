using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Features.GetPayment;

public sealed record GetPaymentResponse(
    Guid Id,
    string Status,
    string CardNumberLastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    long Amount)
{
    public static GetPaymentResponse FromPayment(Payment payment) =>
        new(
            payment.Id,
            payment.Status.ToString(),
            payment.CardNumberLastFour,
            payment.ExpiryMonth,
            payment.ExpiryYear,
            payment.Amount.Currency,
            payment.Amount.Amount);
}

