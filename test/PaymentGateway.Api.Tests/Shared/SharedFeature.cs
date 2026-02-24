using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;

namespace PaymentGateway.Api.Tests.Shared;

[FeatureDescription("Result type and supported currencies invariants")]
[Label("Shared")]
public partial class SharedFeature
{
    [Scenario]
    [Label("Success result contains value")]
    public async Task Success_result_contains_value()
    {
        await Runner.RunScenarioAsync(
            When_a_success_result_is_created,
            Then_IsSuccess_is_true,
            Then_the_value_is_accessible);
    }

    [Scenario]
    [Label("Failure result contains error")]
    public async Task Failure_result_contains_error()
    {
        await Runner.RunScenarioAsync(
            When_a_failure_result_is_created,
            Then_IsFailure_is_true,
            Then_the_error_is_accessible);
    }

    [Scenario]
    [Label("Match routes to correct branch")]
    public async Task Match_routes_to_correct_branch()
    {
        await Runner.RunScenarioAsync(
            When_a_success_result_is_matched,
            Then_the_success_branch_is_invoked);
    }

    [Scenario]
    [Label("Only USD EUR GBP are supported")]
    public async Task Only_three_currencies_are_supported()
    {
        await Runner.RunScenarioAsync(
            Then_USD_is_supported,
            Then_EUR_is_supported,
            Then_GBP_is_supported,
            Then_JPY_is_not_supported,
            Then_null_is_not_supported);
    }

    [Scenario]
    [Label("Money rejects invalid currency")]
    public async Task Money_rejects_invalid_currency()
    {
        await Runner.RunScenarioAsync(
            When_money_is_created_with_invalid_currency_then_it_throws);
    }

    [Scenario]
    [Label("Money rejects zero amount")]
    public async Task Money_rejects_zero_amount()
    {
        await Runner.RunScenarioAsync(
            When_money_is_created_with_zero_amount_then_it_throws);
    }
}
