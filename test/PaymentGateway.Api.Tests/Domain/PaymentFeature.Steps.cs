using System.Reflection;

using LightBDD.XUnit2;

using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Features.GetPayment;
using PaymentGateway.Api.Shared;
using PaymentGateway.Api.Tests.Utilities;

using Shouldly;

namespace PaymentGateway.Api.Tests.Domain;

public partial class PaymentFeature : FeatureFixture
{
    private readonly PaymentFaker _faker = new();
    private Payment? _payment;
    private string _cardLastFour = null!;
    private int _expiryMonth;
    private int _expiryYear;
    private Money _amount = null!;
    private string _authorisationCode = null!;

    private Task Given_valid_payment_details()
    {
        _cardLastFour = _faker.CardLastFour();
        var (month, year) = _faker.FutureExpiry();
        _expiryMonth = month;
        _expiryYear = year;
        _amount = _faker.ValidMoney();
        _authorisationCode = _faker.AuthorisationCode();
        return Task.CompletedTask;
    }

    private Task Given_invalid_card_last_four()
    {
        _cardLastFour = "12"; 
        var (month, year) = _faker.FutureExpiry();
        _expiryMonth = month;
        _expiryYear = year;
        _amount = _faker.ValidMoney();
        _authorisationCode = _faker.AuthorisationCode();
        return Task.CompletedTask;
    }

    private Task When_an_authorised_payment_is_created()
    {
        _payment = Payment.Create(
            _cardLastFour, _expiryMonth, _expiryYear, _amount,
            PaymentStatus.Authorized, _authorisationCode);
        return Task.CompletedTask;
    }

    private Task When_a_declined_payment_is_created()
    {
        _payment = Payment.Create(
            _cardLastFour, _expiryMonth, _expiryYear, _amount,
            PaymentStatus.Declined);
        return Task.CompletedTask;
    }

    private Task When_a_rejected_payment_is_created()
    {
        _payment = Payment.CreateRejected(
            _cardLastFour, _expiryMonth, _expiryYear, _amount);
        return Task.CompletedTask;
    }

    private Task When_creating_payment_should_throw_argument_exception()
    {
        Should.Throw<ArgumentException>(() =>
            Payment.Create(
                _cardLastFour, _expiryMonth, _expiryYear, _amount,
                PaymentStatus.Authorized, _authorisationCode));
        return Task.CompletedTask;
    }

    private Task Then_the_payment_has_authorised_status()
    {
        _payment!.Status.ShouldBe(PaymentStatus.Authorized);
        return Task.CompletedTask;
    }

    private Task Then_the_payment_has_declined_status()
    {
        _payment!.Status.ShouldBe(PaymentStatus.Declined);
        return Task.CompletedTask;
    }

    private Task Then_the_payment_has_rejected_status()
    {
        _payment!.Status.ShouldBe(PaymentStatus.Rejected);
        return Task.CompletedTask;
    }

    private Task Then_the_payment_stores_last_four_digits()
    {
        _payment!.CardNumberLastFour.ShouldBe(_cardLastFour);
        return Task.CompletedTask;
    }

    private Task Then_the_payment_stores_the_authorisation_code()
    {
        _payment!.AuthorisationCode.ShouldBe(_authorisationCode);
        return Task.CompletedTask;
    }

    private Task Then_the_payment_has_no_authorisation_code()
    {
        _payment!.AuthorisationCode.ShouldBeNull();
        return Task.CompletedTask;
    }

    private Task Then_the_payment_has_a_non_empty_id()
    {
        _payment!.Id.ShouldNotBe(Guid.Empty);
        return Task.CompletedTask;
    }

    private Payment? _payment2;
    private GetPaymentResponse? _getResponse;

    private Task Then_the_card_last_four_has_exactly_four_digits()
    {
        _payment!.CardNumberLastFour.Length.ShouldBe(4);
        _payment.CardNumberLastFour.All(char.IsDigit).ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_the_entity_has_no_full_card_number_property()
    {
        var type = typeof(Payment);
        type.GetProperty("CardNumber").ShouldBeNull(
            "Payment entity must not expose a full CardNumber property — PCI compliance.");
        return Task.CompletedTask;
    }

    private Task Then_the_entity_has_no_cvv_property()
    {
        var type = typeof(Payment);
        var cvvProperties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.Name.Contains("Cvv", StringComparison.OrdinalIgnoreCase))
            .ToList();
        cvvProperties.ShouldBeEmpty(
            "Payment entity must not store CVV — PCI compliance.");
        return Task.CompletedTask;
    }

    private Task When_two_payments_are_created()
    {
        _payment = Payment.Create(
            _cardLastFour, _expiryMonth, _expiryYear, _amount,
            PaymentStatus.Authorized, _authorisationCode);
        _payment2 = Payment.Create(
            _cardLastFour, _expiryMonth, _expiryYear, _amount,
            PaymentStatus.Authorized, _authorisationCode);
        return Task.CompletedTask;
    }

    private Task Then_the_two_payments_have_different_ids()
    {
        _payment!.Id.ShouldNotBe(_payment2!.Id);
        return Task.CompletedTask;
    }

    private Task When_the_get_response_is_mapped()
    {
        _getResponse = GetPaymentResponse.FromPayment(_payment!);
        return Task.CompletedTask;
    }

    private Task Then_the_get_response_has_only_last_four_digits()
    {
        _getResponse!.CardNumberLastFour.Length.ShouldBe(4);
        typeof(GetPaymentResponse).GetProperty("CardNumber").ShouldBeNull(
            "GET response must not expose a full CardNumber property.");
        return Task.CompletedTask;
    }
}
