using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;

namespace PaymentGateway.Api.Tests.Infrastructure;

[FeatureDescription("BankClient resilience and error handling")]
[Label("BankClient")]
public partial class BankClientFeature
{
    [Scenario]
    [Label("Network failure returns bank unavailable")]
    public async Task Network_failure_returns_bank_unavailable()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_throws_a_network_exception,
            When_the_bank_client_processes_a_payment,
            Then_the_result_is_a_failure,
            Then_the_error_code_is_bank_unavailable);
    }

    [Scenario]
    [Label("Timeout returns bank unavailable")]
    public async Task Timeout_returns_bank_unavailable()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_times_out,
            When_the_bank_client_processes_a_payment,
            Then_the_result_is_a_failure,
            Then_the_error_code_is_bank_unavailable);
    }

    [Scenario]
    [Label("Bank 503 returns bank unavailable")]
    public async Task Bank_503_returns_bank_unavailable()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_returns_503,
            When_the_bank_client_processes_a_payment,
            Then_the_result_is_a_failure,
            Then_the_error_code_is_bank_unavailable);
    }

    [Scenario]
    [Label("Bank 400 returns bank rejected")]
    public async Task Bank_400_returns_bank_rejected()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_returns_400,
            When_the_bank_client_processes_a_payment,
            Then_the_result_is_a_failure,
            Then_the_error_code_is_bank_rejected);
    }

    [Scenario]
    [Label("Bank authorised response is deserialised correctly")]
    public async Task Bank_authorised_response_is_deserialised()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_returns_authorised,
            When_the_bank_client_processes_a_payment,
            Then_the_result_is_successful,
            Then_the_response_is_authorised_with_code);
    }

    [Scenario]
    [Label("Unexpected status code returns internal error")]
    public async Task Unexpected_status_code_returns_internal_error()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_returns_500,
            When_the_bank_client_processes_a_payment,
            Then_the_result_is_a_failure,
            Then_the_error_code_is_internal);
    }

    [Scenario]
    [Label("Full PAN never appears in log messages")]
    public async Task Full_pan_never_appears_in_logs()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_returns_authorised,
            When_the_bank_client_processes_a_payment,
            Then_no_log_message_contains_the_full_card_number);
    }
}
