using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Domain;

public sealed class Payment
{
    public Guid Id { get; }
    public PaymentStatus Status { get; }
    public string CardNumberLastFour { get; }
    public int ExpiryMonth { get; }
    public int ExpiryYear { get; }
    public Money Amount { get; }
    public string? AuthorisationCode { get; }
    public DateTime CreatedAtUtc { get; }

    private Payment(
        Guid id,
        PaymentStatus status,
        string cardNumberLastFour,
        int expiryMonth,
        int expiryYear,
        Money amount,
        string? authorisationCode,
        DateTime createdAtUtc)
    {
        Id = id;
        Status = status;
        CardNumberLastFour = cardNumberLastFour;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        Amount = amount;
        AuthorisationCode = authorisationCode;
        CreatedAtUtc = createdAtUtc;
    }

    public static Payment Create(
        string cardNumberLastFour,
        int expiryMonth,
        int expiryYear,
        Money amount,
        PaymentStatus status,
        string? authorisationCode = null)
    {
        GuardCardLastFour(cardNumberLastFour);
        GuardExpiry(expiryMonth, expiryYear);

        return new Payment(
            Guid.NewGuid(),
            status,
            cardNumberLastFour,
            expiryMonth,
            expiryYear,
            amount,
            authorisationCode,
            DateTime.UtcNow);
    }

    public static Payment CreateRejected(
        string cardNumberLastFour,
        int expiryMonth,
        int expiryYear,
        Money amount)
    {
        return new Payment(
            Guid.NewGuid(),
            PaymentStatus.Rejected,
            cardNumberLastFour,
            expiryMonth,
            expiryYear,
            amount,
            authorisationCode: null,
            DateTime.UtcNow);
    }

    private static void GuardCardLastFour(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 4 || !value.All(char.IsDigit))
            throw new ArgumentException("Card number last four must be exactly 4 digits.", nameof(value));
    }

    private static void GuardExpiry(int month, int year)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
    }
}
