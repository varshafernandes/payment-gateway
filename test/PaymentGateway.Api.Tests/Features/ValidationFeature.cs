using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;

namespace PaymentGateway.Api.Tests.Features;

[FeatureDescription("Payment command validation rules")]
[Label("Validation")]
public partial class ValidationFeature
{
    [Scenario]
    [Label("Valid command passes validation")]
    public async Task Valid_command_passes_validation()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Empty card number fails with required message")]
    public async Task Empty_card_number_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_empty_card_number,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_number_is_required,
            Then_the_error_is_on_CardNumber_field);
    }

    [Scenario]
    [Label("Non-numeric card number fails with digits only message")]
    public async Task Non_numeric_card_number_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_non_numeric_card,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_number_must_contain_digits_only);
    }

    [Scenario]
    [Label("Card number too short fails with length message")]
    public async Task Card_number_too_short_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_short_card_number,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_number_must_be_between_14_and_19_digits);
    }

    [Scenario]
    [Label("Expired card fails with expired message")]
    public async Task Expired_card_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_expired_card,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_has_expired);
    }

    [Scenario]
    [Label("Expiry month out of range fails with range message")]
    public async Task Expiry_month_out_of_range_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_month_13,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_expiry_month_must_be_between_1_and_12,
            Then_the_error_is_on_ExpiryMonth_field);
    }

    [Scenario]
    [Label("Unsupported currency fails with supported currencies message")]
    public async Task Unsupported_currency_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_unsupported_currency,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_is_on_Currency_field);
    }

    [Scenario]
    [Label("Zero amount fails with greater than zero message")]
    public async Task Zero_amount_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_zero_amount,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_amount_must_be_greater_than_zero,
            Then_the_error_is_on_Amount_field);
    }

    [Scenario]
    [Label("Empty CVV fails with required message")]
    public async Task Empty_cvv_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_empty_cvv,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_cvv_is_required,
            Then_the_error_is_on_Cvv_field);
    }

    [Scenario]
    [Label("CVV too long fails with length message")]
    public async Task Cvv_too_long_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_five_digit_cvv,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_cvv_must_be_3_or_4_digits);
    }

    [Scenario]
    [Label("All three supported currencies pass")]
    public async Task All_supported_currencies_pass()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_USD,
            When_the_command_is_validated,
            Then_the_result_is_valid,
            Given_a_command_with_EUR,
            When_the_command_is_validated,
            Then_the_result_is_valid,
            Given_a_command_with_GBP,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Null card number fails with required message")]
    public async Task Null_card_number_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_null_card_number,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_number_is_required,
            Then_the_error_is_on_CardNumber_field);
    }

    [Scenario]
    [Label("Card number with 20 digits fails")]
    public async Task Card_number_too_long_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_20_digit_card_number,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_number_must_be_between_14_and_19_digits);
    }

    [Scenario]
    [Label("Card number with exactly 14 digits passes")]
    public async Task Card_number_exactly_14_digits_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_14_digit_card_number,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Card number with exactly 19 digits passes")]
    public async Task Card_number_exactly_19_digits_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_19_digit_card_number,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Card number with 13 digits fails")]
    public async Task Card_number_13_digits_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_13_digit_card_number,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_number_must_be_between_14_and_19_digits);
    }

    [Scenario]
    [Label("Card number with spaces fails")]
    public async Task Card_number_with_spaces_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_card_number_containing_spaces,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_number_must_contain_digits_only);
    }

    [Scenario]
    [Label("Current year with past month fails")]
    public async Task Current_year_past_month_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_current_year_and_past_month,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_has_expired);
    }

    [Scenario]
    [Label("Current year with current month passes")]
    public async Task Current_year_current_month_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_current_year_and_current_month,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Future year with lower month passes")]
    public async Task Future_year_lower_month_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_future_year_and_january,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Past expiry year fails")]
    public async Task Past_expiry_year_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_past_year,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_expiry_year_must_not_be_in_the_past,
            Then_the_error_is_on_ExpiryYear_field);
    }

    [Scenario]
    [Label("Month zero fails")]
    public async Task Month_zero_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_month_zero,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_expiry_month_must_be_between_1_and_12,
            Then_the_error_is_on_ExpiryMonth_field);
    }

    [Scenario]
    [Label("Lowercase currency passes (case-insensitive)")]
    public async Task Lowercase_currency_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_lowercase_usd,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Two-character currency fails")]
    public async Task Two_char_currency_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_two_char_currency,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_currency_must_be_3_letter_iso,
            Then_the_error_is_on_Currency_field);
    }

    [Scenario]
    [Label("Four-character currency fails")]
    public async Task Four_char_currency_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_four_char_currency,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_currency_must_be_3_letter_iso,
            Then_the_error_is_on_Currency_field);
    }

    [Scenario]
    [Label("Negative amount fails")]
    public async Task Negative_amount_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_negative_amount,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_amount_must_be_greater_than_zero,
            Then_the_error_is_on_Amount_field);
    }

    [Scenario]
    [Label("Max long amount passes")]
    public async Task Max_long_amount_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_max_long_amount,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Two-digit CVV fails")]
    public async Task Two_digit_cvv_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_two_digit_cvv,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_cvv_must_be_3_or_4_digits,
            Then_the_error_is_on_Cvv_field);
    }

    [Scenario]
    [Label("CVV with letters fails")]
    public async Task Cvv_with_letters_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_letters_in_cvv,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_cvv_must_contain_digits_only,
            Then_the_error_is_on_Cvv_field);
    }

    [Scenario]
    [Label("Three-digit CVV passes")]
    public async Task Three_digit_cvv_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_three_digit_cvv,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Four-digit CVV passes")]
    public async Task Four_digit_cvv_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_four_digit_cvv,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Card number with leading and trailing whitespace fails")]
    public async Task Card_number_with_leading_trailing_whitespace_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_whitespace_padded_card_number,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_number_must_contain_digits_only);
    }

    [Scenario]
    [Label("Currency with leading and trailing whitespace fails")]
    public async Task Currency_with_whitespace_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_whitespace_padded_currency,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_currency_must_be_3_letter_iso,
            Then_the_error_is_on_Currency_field);
    }

    [Scenario]
    [Label("Missing expiry month fails with required message")]
    public async Task Missing_expiry_month_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_null_expiry_month,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_expiry_month_is_required,
            Then_the_error_is_on_ExpiryMonth_field);
    }

    [Scenario]
    [Label("Missing expiry year fails with required message")]
    public async Task Missing_expiry_year_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_null_expiry_year,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_expiry_year_is_required,
            Then_the_error_is_on_ExpiryYear_field);
    }

    [Scenario]
    [Label("Missing amount fails with required message")]
    public async Task Missing_amount_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_null_amount,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_amount_is_required,
            Then_the_error_is_on_Amount_field);
    }
}
