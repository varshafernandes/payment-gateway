using FluentValidation.Results;

using LightBDD.XUnit2;

using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Features.ProcessPayment;
using PaymentGateway.Api.Tests.Utilities;

using Shouldly;

namespace PaymentGateway.Api.Tests.Features;

public partial class ValidationFeature : FeatureFixture
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));

    private readonly ProcessPaymentCommandValidator _validator = new(Clock);
    private ProcessPaymentCommand _command = null!;
    private ValidationResult _validationResult = new();

    private Task Given_a_valid_command()
    {
        _command = PaymentCommandBuilder.Valid(Clock).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_empty_card_number()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber("").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_non_numeric_card()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber("ABCD123456789012").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_short_card_number()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber("12345").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_expired_card()
    {
        _command = PaymentCommandBuilder.Valid(Clock)
            .WithExpiryMonth(1)
            .WithExpiryYear(2020)
            .Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_month_13()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithExpiryMonth(13).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_unsupported_currency()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCurrency("JPY").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_zero_amount()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithAmount(0).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_empty_cvv()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCvv("").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_five_digit_cvv()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCvv("12345").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_USD()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCurrency("USD").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_EUR()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCurrency("EUR").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_GBP()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCurrency("GBP").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_null_card_number()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber(null!).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_20_digit_card_number()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber("12345678901234567890").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_14_digit_card_number()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber("12345678901234").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_19_digit_card_number()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber("1234567890123456789").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_13_digit_card_number()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber("1234567890123").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_card_number_containing_spaces()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber("1234 5678 9012 3456").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_current_year_and_past_month()
    {
        _command = PaymentCommandBuilder.Valid(Clock)
            .WithExpiryMonth(5)
            .WithExpiryYear(2025)
            .Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_current_year_and_current_month()
    {
        _command = PaymentCommandBuilder.Valid(Clock)
            .WithExpiryMonth(6)
            .WithExpiryYear(2025)
            .Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_future_year_and_january()
    {
        _command = PaymentCommandBuilder.Valid(Clock)
            .WithExpiryMonth(1)
            .WithExpiryYear(2026)
            .Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_past_year()
    {
        _command = PaymentCommandBuilder.Valid(Clock)
            .WithExpiryMonth(12)
            .WithExpiryYear(2024)
            .Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_month_zero()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithExpiryMonth(0).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_lowercase_usd()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCurrency("usd").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_two_char_currency()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCurrency("US").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_four_char_currency()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCurrency("USDX").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_negative_amount()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithAmount(-1).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_max_long_amount()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithAmount(long.MaxValue).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_two_digit_cvv()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCvv("12").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_letters_in_cvv()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCvv("abc").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_three_digit_cvv()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCvv("123").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_four_digit_cvv()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCvv("1234").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_whitespace_padded_card_number()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCardNumber(" 12345678901234 ").Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_whitespace_padded_currency()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithCurrency(" USD ").Build();
        return Task.CompletedTask;
    }

    private async Task When_the_command_is_validated()
    {
        _validationResult = await _validator.ValidateAsync(_command);
    }

    private Task Then_the_result_is_valid()
    {
        _validationResult.IsValid.ShouldBeTrue(
            $"Expected valid but got errors: {string.Join("; ", _validationResult.Errors.Select(e => e.ErrorMessage))}");
        return Task.CompletedTask;
    }

    private Task Then_the_result_is_invalid()
    {
        _validationResult.IsValid.ShouldBeFalse("Expected validation to fail but it passed.");
        return Task.CompletedTask;
    }

    private Task Then_the_error_says_card_number_is_required()
        => AssertErrorMessage("Card number is required.");

    private Task Then_the_error_says_card_number_must_contain_digits_only()
        => AssertErrorMessage("Card number must contain digits only.");

    private Task Then_the_error_says_card_number_must_be_between_14_and_19_digits()
        => AssertErrorMessage("Card number must be between 14 and 19 digits.");

    private Task Then_the_error_says_card_has_expired()
        => AssertErrorMessage("Card has expired.");

    private Task Then_the_error_says_expiry_month_must_be_between_1_and_12()
        => AssertErrorMessage("Expiry month must be between 1 and 12.");

    private Task Then_the_error_says_amount_must_be_greater_than_zero()
        => AssertErrorMessage("Amount must be greater than zero.");

    private Task Then_the_error_says_cvv_is_required()
        => AssertErrorMessage("CVV is required.");

    private Task Then_the_error_says_cvv_must_be_3_or_4_digits()
        => AssertErrorMessage("CVV must be 3 or 4 digits.");

    private Task Then_the_error_says_expiry_year_must_not_be_in_the_past()
        => AssertErrorMessage("Expiry year must not be in the past.");

    private Task Then_the_error_says_currency_must_be_3_letter_iso()
        => AssertErrorMessage("Currency must be a 3-letter ISO code.");

    private Task Then_the_error_says_cvv_must_contain_digits_only()
        => AssertErrorMessage("CVV must contain digits only.");

    private Task Then_the_error_is_on_CardNumber_field()
        => AssertErrorOnField("CardNumber");

    private Task Then_the_error_is_on_ExpiryMonth_field()
        => AssertErrorOnField("ExpiryMonth");

    private Task Then_the_error_is_on_ExpiryYear_field()
        => AssertErrorOnField("ExpiryYear");

    private Task Then_the_error_is_on_Currency_field()
        => AssertErrorOnField("Currency");

    private Task Then_the_error_is_on_Amount_field()
        => AssertErrorOnField("Amount");

    private Task Then_the_error_is_on_Cvv_field()
        => AssertErrorOnField("Cvv");

    private Task AssertErrorMessage(string expectedMessage)
    {
        _validationResult.Errors
            .ShouldContain(e => e.ErrorMessage == expectedMessage,
                $"Expected error '{expectedMessage}' but got: {string.Join("; ", _validationResult.Errors.Select(e => e.ErrorMessage))}");
        return Task.CompletedTask;
    }

    private Task AssertErrorOnField(string fieldName)
    {
        _validationResult.Errors
            .ShouldContain(e => e.PropertyName == fieldName,
                $"Expected error on field '{fieldName}' but errors were on: {string.Join(", ", _validationResult.Errors.Select(e => e.PropertyName))}");
        return Task.CompletedTask;
    }
}
