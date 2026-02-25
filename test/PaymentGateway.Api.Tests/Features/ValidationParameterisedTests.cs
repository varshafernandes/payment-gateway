using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Features.ProcessPayment;
using PaymentGateway.Api.Tests.Utilities;

using Shouldly;

namespace PaymentGateway.Api.Tests.Features;

public sealed class ValidationParameterisedTests
{
    private static readonly FakeTimeProvider Clock =
        new(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));

    private readonly ProcessPaymentCommandValidator _validator = new(Clock);

    private ProcessPaymentCommand Build(Action<PaymentCommandBuilder> configure)
    {
        var builder = PaymentCommandBuilder.Valid(Clock);
        configure(builder);
        return builder.Build();
    }

    [Theory]
    [InlineData(null,                   "Card number is required.")]
    [InlineData("",                     "Card number is required.")]
    [InlineData("ABCD123456789012",     "Card number must contain digits only.")]
    [InlineData("1234 5678 9012 3456",  "Card number must contain digits only.")]
    [InlineData(" 12345678901234 ",     "Card number must contain digits only.")]
    [InlineData("1234567890123",        "Card number must be between 14 and 19 digits.")]  
    [InlineData("12345678901234567890", "Card number must be between 14 and 19 digits.")]  
    public async Task Invalid_card_number_fails(string? cardNumber, string expectedError)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithCardNumber(cardNumber)));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData("12345678901234")]      
    [InlineData("1234567890123456")]    
    [InlineData("1234567890123456789")] 
    public async Task Valid_card_number_passes(string cardNumber)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithCardNumber(cardNumber)));
        result.Errors.ShouldNotContain(e => e.PropertyName == "CardNumber");
    }

    [Theory]
    [InlineData(null,    "CVV is required.")]
    [InlineData("",      "CVV is required.")]
    [InlineData("12",    "CVV must be 3 or 4 digits.")]
    [InlineData("12345", "CVV must be 3 or 4 digits.")]
    [InlineData("abc",   "CVV must contain digits only.")]
    public async Task Invalid_cvv_fails(string? cvv, string expectedError)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithCvv(cvv)));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData("123")]  
    [InlineData("1234")] 
    public async Task Valid_cvv_passes(string cvv)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithCvv(cvv)));
        result.Errors.ShouldNotContain(e => e.PropertyName == "Cvv");
    }

    [Theory]
    [InlineData("US",    "Currency must be a 3-letter ISO code.")]  
    [InlineData("USDX",  "Currency must be a 3-letter ISO code.")]  
    [InlineData(" USD ", "Currency must be a 3-letter ISO code.")]  
    public async Task Invalid_currency_length_fails(string currency, string expectedError)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithCurrency(currency)));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData("JPY")] 
    [InlineData("CHF")] 
    public async Task Unsupported_three_letter_currency_fails(string currency)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithCurrency(currency)));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("usd")] 
    [InlineData("eur")]
    [InlineData("gbp")]
    public async Task Supported_currency_passes(string currency)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithCurrency(currency)));
        result.Errors.ShouldNotContain(e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData(null, "Expiry month is required.")]
    [InlineData(0,    "Expiry month must be between 1 and 12.")]
    [InlineData(13,   "Expiry month must be between 1 and 12.")]
    public async Task Invalid_expiry_month_fails(int? month, string expectedError)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithExpiryMonth(month)));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(1)]   
    [InlineData(6)]   
    [InlineData(12)] 
    public async Task Valid_expiry_month_passes(int month)
    {
        var result = await _validator.ValidateAsync(
            Build(b => b.WithExpiryMonth(month).WithExpiryYear(2099)));
        result.Errors.ShouldNotContain(e => e.PropertyName == "ExpiryMonth");
    }

    [Fact]
    public async Task Null_expiry_year_fails()
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithExpiryYear(null)));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Expiry year is required.");
    }

    [Theory]
    [InlineData(null, "Amount is required.")]
    [InlineData(0L,   "Amount must be greater than zero.")]
    [InlineData(-1L,  "Amount must be greater than zero.")]
    public async Task Invalid_amount_fails(long? amount, string expectedError)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithAmount(amount)));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(1L)]            
    [InlineData(999_999L)]      
    [InlineData(long.MaxValue)] 
    public async Task Valid_amount_passes(long amount)
    {
        var result = await _validator.ValidateAsync(Build(b => b.WithAmount(amount)));
        result.Errors.ShouldNotContain(e => e.PropertyName == "Amount");
    }
}
