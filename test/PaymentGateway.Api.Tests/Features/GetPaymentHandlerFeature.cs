using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;

namespace PaymentGateway.Api.Tests.Features;

[FeatureDescription("Retrieving a previously processed payment")]
[Label("GetPaymentHandler")]
public partial class GetPaymentHandlerFeature
{
    [Scenario]
    [Label("Existing payment is returned")]
    public async Task Existing_payment_is_returned()
    {
        await Runner.RunScenarioAsync(
            Given_a_payment_exists_in_the_repository,
            When_the_payment_is_requested,
            Then_the_result_is_successful,
            Then_the_response_matches_the_stored_payment);
    }

    [Scenario]
    [Label("Non-existent payment returns not found")]
    public async Task Non_existent_payment_returns_not_found()
    {
        await Runner.RunScenarioAsync(
            Given_no_payment_exists_for_the_id,
            When_the_payment_is_requested,
            Then_the_result_is_a_failure,
            Then_the_error_code_is_payment_not_found);
    }
}
