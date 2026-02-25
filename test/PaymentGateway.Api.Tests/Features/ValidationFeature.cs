using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;

namespace PaymentGateway.Api.Tests.Features;

[FeatureDescription("Payment command validation rules — date-sensitive expiry")]
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
    [Label("Card expired in the past fails")]
    public async Task Expired_card_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_expired_card,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_has_expired);
    }

    [Scenario]
    [Label("Current year with already-past month fails")]
    public async Task Current_year_past_month_fails()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_current_year_and_past_month,
            When_the_command_is_validated,
            Then_the_result_is_invalid,
            Then_the_error_says_card_has_expired);
    }

    [Scenario]
    [Label("Current year with current month passes (card valid until end of month)")]
    public async Task Current_year_current_month_passes()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_current_year_and_current_month,
            When_the_command_is_validated,
            Then_the_result_is_valid);
    }

    [Scenario]
    [Label("Future year with earlier month passes")]
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
}
