namespace PaymentGateway.Api.IntegrationTests;

[FeatureDescription("Retrieving a payment through the API")]
[Label("GetPaymentApi")]
public partial class GetPaymentApiFeature
{
    [Scenario]
    [Label("Existing payment is returned via API")]
    public async Task Existing_payment_is_returned_via_api()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_authorise,
            Given_a_payment_has_been_created,
            When_the_payment_is_retrieved_by_id,
            Then_the_response_status_code_is_200,
            Then_the_retrieved_payment_matches_the_original);
    }

    [Scenario]
    [Label("Non-existent payment returns 404")]
    public async Task Non_existent_payment_returns_404()
    {
        await Runner.RunScenarioAsync(
            When_a_non_existent_payment_is_requested,
            Then_the_response_status_code_is_404);
    }

    [Scenario]
    [Label("Invalid GUID format returns 404 from route constraint")]
    public async Task Invalid_guid_format_returns_404()
    {
        await Runner.RunScenarioAsync(
            When_an_invalid_guid_is_requested,
            Then_the_response_status_code_is_404);
    }

    [Scenario]
    [Label("Retrieved payment response masks card number")]
    public async Task Retrieved_payment_masks_card_number()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_authorise,
            Given_a_payment_has_been_created,
            When_the_payment_is_retrieved_by_id,
            Then_the_response_status_code_is_200,
            Then_the_get_response_body_does_not_contain_full_card_number,
            Then_the_get_response_contains_only_last_four_digits);
    }
}
