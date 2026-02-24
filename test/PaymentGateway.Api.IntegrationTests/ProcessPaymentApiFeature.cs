namespace PaymentGateway.Api.IntegrationTests;

/// <summary>
/// Integration tests for POST /api/payments.
/// Exercises the full HTTP pipeline with WireMock standing in for the bank.
/// </summary>
[FeatureDescription("Processing a payment through the API")]
[Label("ProcessPaymentApi")]
public partial class ProcessPaymentApiFeature
{
    [Scenario]
    [Label("Authorised payment end-to-end")]
    public async Task Authorised_payment_end_to_end()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_authorise,
            When_a_valid_payment_request_is_posted,
            Then_the_response_status_code_is_200,
            Then_the_response_status_is_Authorized,
            Then_the_response_contains_the_correct_card_details,
            Then_the_payment_can_be_retrieved_by_id);
    }

    [Scenario]
    [Label("Declined payment end-to-end")]
    public async Task Declined_payment_end_to_end()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_decline,
            When_a_valid_payment_request_is_posted,
            Then_the_response_status_code_is_200,
            Then_the_response_status_is_Declined);
    }

    [Scenario]
    [Label("Bank unavailable returns 502")]
    public async Task Bank_unavailable_returns_502()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_return_503,
            When_a_valid_payment_request_is_posted,
            Then_the_response_status_code_is_502);
    }

    [Scenario]
    [Label("Invalid request returns 400 with structured errors")]
    public async Task Invalid_request_returns_400()
    {
        await Runner.RunScenarioAsync(
            When_an_invalid_payment_request_is_posted,
            Then_the_response_status_code_is_400,
            Then_the_response_contains_structured_validation_errors);
    }

    [Scenario]
    [Label("Health check returns 200")]
    public async Task Health_check_returns_200()
    {
        await Runner.RunScenarioAsync(
            When_the_health_endpoint_is_called,
            Then_the_response_status_code_is_200);
    }

    [Scenario]
    [Label("Malformed JSON returns 400")]
    public async Task Malformed_json_returns_400()
    {
        await Runner.RunScenarioAsync(
            When_a_malformed_json_body_is_posted,
            Then_the_response_status_code_is_400,
            Then_the_response_body_contains_validation_error);
    }

    [Scenario]
    [Label("Bank returns 400 results in 400 with bank rejected code")]
    public async Task Bank_400_returns_400()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_return_400,
            When_a_valid_payment_request_is_posted,
            Then_the_response_status_code_is_400,
            Then_the_response_body_contains_bank_rejected_error);
    }

    [Scenario]
    [Label("Validation failure does not call the bank")]
    public async Task Validation_failure_does_not_call_bank()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_authorise,
            When_an_invalid_payment_request_is_posted,
            Then_the_response_status_code_is_400,
            Then_the_bank_was_not_called);
    }

    [Scenario]
    [Label("Response never returns full PAN")]
    public async Task Response_never_returns_full_pan()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_authorise,
            When_a_valid_payment_request_is_posted,
            Then_the_response_status_code_is_200,
            Then_the_response_body_does_not_contain_full_card_number);
    }

    [Scenario]
    [Label("JSON contract uses camelCase property names")]
    public async Task Json_contract_uses_camel_case()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_authorise,
            When_a_valid_payment_request_is_posted,
            Then_the_response_status_code_is_200,
            Then_the_json_uses_camelCase_property_names,
            Then_the_json_does_not_expose_internal_fields);
    }

    [Scenario]
    [Label("Status value is exactly 'Authorized' — correct casing")]
    public async Task Status_value_casing_is_correct()
    {
        await Runner.RunScenarioAsync(
            Given_the_bank_will_authorise,
            When_a_valid_payment_request_is_posted,
            Then_the_response_status_code_is_200,
            Then_the_raw_status_value_is_exactly_Authorized);
    }
}
