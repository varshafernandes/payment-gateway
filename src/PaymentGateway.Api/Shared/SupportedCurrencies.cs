namespace PaymentGateway.Api.Shared;

public static class SupportedCurrencies
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "USD",
        "EUR",
        "GBP"
    };

    public static bool IsValid(string? currency) =>
        !string.IsNullOrWhiteSpace(currency) && All.Contains(currency);
}
