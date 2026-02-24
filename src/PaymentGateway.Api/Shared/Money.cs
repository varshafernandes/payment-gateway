namespace PaymentGateway.Api.Shared;

public sealed record Money
{
    public long Amount { get; }
    public string Currency { get; }

    public Money(long amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        var normalised = currency.ToUpperInvariant();

        if (!SupportedCurrencies.IsValid(normalised))
            throw new ArgumentException($"Currency '{currency}' is not supported. Accepted: {string.Join(", ", SupportedCurrencies.All)}", nameof(currency));

        Amount = amount;
        Currency = normalised;
    }

    public override string ToString() => $"{Currency} {Amount}";
}
