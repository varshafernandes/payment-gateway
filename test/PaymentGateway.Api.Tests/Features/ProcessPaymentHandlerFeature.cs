using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;

namespace PaymentGateway.Api.Tests.Features;

[FeatureDescription("Processing a payment through the handler")]
[Label("ProcessPaymentHandler")]
public partial class ProcessPaymentHandlerFeature
{
    [Scenario]
    [Label("Authorised payment returns success")]
    public async Task Authorised_payment_returns_success()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            Given_the_bank_authorises_the_payment,
            When_the_handler_is_invoked,
            Then_the_result_is_successful,
            Then_the_response_status_is_Authorized,
            Then_the_payment_is_persisted);
    }

    [Scenario]
    [Label("Declined payment returns success with declined status")]
    public async Task Declined_payment_returns_declined_status()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            Given_the_bank_declines_the_payment,
            When_the_handler_is_invoked,
            Then_the_result_is_successful,
            Then_the_response_status_is_Declined,
            Then_the_payment_is_persisted);
    }

    [Scenario]
    [Label("Bank unavailable returns failure")]
    public async Task Bank_unavailable_returns_failure()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            Given_the_bank_is_unavailable,
            When_the_handler_is_invoked,
            Then_the_result_is_a_failure,
            Then_the_error_code_is_bank_unavailable,
            Then_no_payment_is_persisted);
    }

    [Scenario]
    [Label("Response contains correct card last four")]
    public async Task Response_contains_correct_card_last_four()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            Given_the_bank_authorises_the_payment,
            When_the_handler_is_invoked,
            Then_the_response_card_last_four_is_correct);
    }

    [Scenario]
    [Label("Response contains correct amount and currency")]
    public async Task Response_contains_correct_amount_and_currency()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            Given_the_bank_authorises_the_payment,
            When_the_handler_is_invoked,
            Then_the_response_amount_matches_the_command,
            Then_the_response_currency_matches_the_command);
    }

    [Scenario]
    [Label("Bank rejected returns failure with bank rejected code")]
    public async Task Bank_rejected_returns_failure()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            Given_the_bank_rejects_the_payment,
            When_the_handler_is_invoked,
            Then_the_result_is_a_failure,
            Then_the_error_code_is_bank_rejected,
            Then_no_payment_is_persisted);
    }

    [Scenario]
    [Label("Repository exception propagates")]
    public async Task Repository_exception_propagates()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            Given_the_bank_authorises_the_payment,
            Given_the_repository_throws_on_add,
            When_the_handler_is_invoked_expecting_failure,
            Then_the_exception_is_an_InvalidOperationException);
    }

    [Scenario]
    [Label("Authorised payment auth code is non-empty")]
    public async Task Authorised_payment_auth_code_is_non_empty()
    {
        await Runner.RunScenarioAsync(
            Given_a_valid_command,
            Given_the_bank_authorises_the_payment,
            When_the_handler_is_invoked,
            Then_the_result_is_successful,
            Then_the_response_status_is_Authorized,
            Then_the_response_has_a_non_empty_payment_id);
    }

    [Scenario]
    [Label("Expiry month is zero-padded in bank request")]
    public async Task Expiry_month_is_zero_padded_in_bank_request()
    {
        await Runner.RunScenarioAsync(
            Given_a_command_with_single_digit_expiry_month,
            Given_the_bank_authorises_the_payment,
            When_the_handler_is_invoked,
            Then_the_bank_received_zero_padded_expiry);
    }
}
