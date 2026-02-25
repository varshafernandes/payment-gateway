using Bogus;

using PaymentGateway.Api.Features.ProcessPayment;

namespace PaymentGateway.Api.Tests.Utilities;

public sealed class PaymentCommandBuilder
{
    private static readonly Faker SharedFaker = new();

    private string? _cardNumber;
    private int? _expiryMonth;
    private int? _expiryYear;
    private string? _currency;
    private long? _amount;
    private string? _cvv;

    private PaymentCommandBuilder(TimeProvider? timeProvider)
    {
        var now = timeProvider?.GetUtcNow().DateTime ?? DateTime.UtcNow;

        _cardNumber = string.Concat(SharedFaker.Random.Digits(16));
        _expiryMonth = SharedFaker.Random.Int(1, 12);
        _expiryYear = now.Year + SharedFaker.Random.Int(1, 5);
        _currency = SharedFaker.PickRandom("USD", "EUR", "GBP");
        _amount = SharedFaker.Random.Long(1, 999_999);
        _cvv = string.Concat(SharedFaker.Random.Digits(SharedFaker.Random.Int(3, 4)));
    }

    public PaymentCommandBuilder WithCardNumber(string? value) { _cardNumber = value; return this; }
    public PaymentCommandBuilder WithExpiryMonth(int? value) { _expiryMonth = value; return this; }
    public PaymentCommandBuilder WithExpiryYear(int? value) { _expiryYear = value; return this; }
    public PaymentCommandBuilder WithCurrency(string? value) { _currency = value; return this; }
    public PaymentCommandBuilder WithAmount(long? value) { _amount = value; return this; }
    public PaymentCommandBuilder WithCvv(string? value) { _cvv = value; return this; }

    public ProcessPaymentCommand Build() =>
        new(_cardNumber, _expiryMonth, _expiryYear, _currency, _amount, _cvv);

    public static PaymentCommandBuilder Valid(TimeProvider timeProvider) => new(timeProvider);

    public static PaymentCommandBuilder Valid() => new(null);
}
