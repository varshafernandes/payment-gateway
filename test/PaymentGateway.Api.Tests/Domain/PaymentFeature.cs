using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;

namespace PaymentGateway.Api.Tests.Domain;

[FeatureDescription("Payment domain entity creation and validation")]
[Label("Payment")]
public partial class PaymentFeature
{
    [Scenario]
    [Label("Authorised payment stores correct data")]
    public async Task Authorised_payment_stores_correct_data()
    {
        await Runner.RunScenarioAsync(
            Given_valid_payment_details,
            When_an_authorised_payment_is_created,
            Then_the_payment_has_authorised_status,
            Then_the_payment_stores_last_four_digits,
            Then_the_payment_stores_the_authorisation_code,
            Then_the_payment_has_a_non_empty_id);
    }

    [Scenario]
    [Label("Declined payment has no authorisation code")]
    public async Task Declined_payment_has_no_authorisation_code()
    {
        await Runner.RunScenarioAsync(
            Given_valid_payment_details,
            When_a_declined_payment_is_created,
            Then_the_payment_has_declined_status,
            Then_the_payment_has_no_authorisation_code);
    }

    [Scenario]
    [Label("Rejected payment is created without bank involvement")]
    public async Task Rejected_payment_is_created_without_bank()
    {
        await Runner.RunScenarioAsync(
            Given_valid_payment_details,
            When_a_rejected_payment_is_created,
            Then_the_payment_has_rejected_status,
            Then_the_payment_has_no_authorisation_code);
    }

    [Scenario]
    [Label("Invalid last four digits throws")]
    public async Task Invalid_last_four_digits_throws()
    {
        await Runner.RunScenarioAsync(
            Given_invalid_card_last_four,
            When_creating_payment_should_throw_argument_exception);
    }

    [Scenario]
    [Label("Payment only stores last four digits of card")]
    public async Task Payment_only_stores_last_four_digits()
    {
        await Runner.RunScenarioAsync(
            Given_valid_payment_details,
            When_an_authorised_payment_is_created,
            Then_the_card_last_four_has_exactly_four_digits,
            Then_the_entity_has_no_full_card_number_property);
    }

    [Scenario]
    [Label("Payment entity does not store CVV")]
    public async Task Payment_entity_does_not_store_cvv()
    {
        await Runner.RunScenarioAsync(
            Given_valid_payment_details,
            When_an_authorised_payment_is_created,
            Then_the_entity_has_no_cvv_property);
    }

    [Scenario]
    [Label("Two payments have different IDs")]
    public async Task Two_payments_have_different_ids()
    {
        await Runner.RunScenarioAsync(
            Given_valid_payment_details,
            When_two_payments_are_created,
            Then_the_two_payments_have_different_ids);
    }

    [Scenario]
    [Label("GET response never returns full PAN")]
    public async Task Get_response_never_returns_full_pan()
    {
        await Runner.RunScenarioAsync(
            Given_valid_payment_details,
            When_an_authorised_payment_is_created,
            When_the_get_response_is_mapped,
            Then_the_get_response_has_only_last_four_digits);
    }
}
