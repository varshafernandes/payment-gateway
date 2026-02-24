using LightBDD.XUnit2;

using Microsoft.Extensions.Logging;

using NSubstitute;

using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Features.GetPayment;
using PaymentGateway.Api.Infrastructure.Repositories;
using PaymentGateway.Api.Shared;
using PaymentGateway.Api.Tests.Utilities;

using Shouldly;

namespace PaymentGateway.Api.Tests.Features;

public partial class GetPaymentHandlerFeature : FeatureFixture
{
    private readonly PaymentFaker _faker = new();
    private readonly IPaymentRepository _repository = Substitute.For<IPaymentRepository>();
    private readonly ILogger<GetPaymentQueryHandler> _logger =
        Substitute.For<ILogger<GetPaymentQueryHandler>>();

    private Guid _paymentId;
    private Payment? _storedPayment;
    private Result<GetPaymentResponse>? _result;

    private Task Given_a_payment_exists_in_the_repository()
    {
        var cardLastFour = _faker.CardLastFour();
        var (month, year) = _faker.FutureExpiry();
        var amount = _faker.ValidMoney();

        _storedPayment = Payment.Create(
            cardLastFour, month, year, amount,
            PaymentStatus.Authorized, _faker.AuthorisationCode());

        _paymentId = _storedPayment.Id;
        _repository.GetById(_paymentId).Returns(_storedPayment);

        return Task.CompletedTask;
    }

    private Task Given_no_payment_exists_for_the_id()
    {
        _paymentId = Guid.NewGuid();
        _repository.GetById(_paymentId).Returns((Payment?)null);
        return Task.CompletedTask;
    }

    private async Task When_the_payment_is_requested()
    {
        var handler = new GetPaymentQueryHandler(_repository, _logger);
        _result = await handler.Handle(new GetPaymentQuery(_paymentId), CancellationToken.None);
    }

    private Task Then_the_result_is_successful()
    {
        _result.ShouldNotBeNull();
        _result!.IsSuccess.ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_the_result_is_a_failure()
    {
        _result.ShouldNotBeNull();
        _result!.IsFailure.ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_the_error_code_is_payment_not_found()
    {
        _result!.Error!.Code.ShouldBe(ErrorCodes.PaymentNotFound);
        return Task.CompletedTask;
    }

    private Task Then_the_response_matches_the_stored_payment()
    {
        var response = _result!.Value!;

        response.Id.ShouldBe(_storedPayment!.Id);
        response.Status.ShouldBe(PaymentStatus.Authorized.ToString());
        response.CardNumberLastFour.ShouldBe(_storedPayment.CardNumberLastFour);
        response.ExpiryMonth.ShouldBe(_storedPayment.ExpiryMonth);
        response.ExpiryYear.ShouldBe(_storedPayment.ExpiryYear);
        response.Amount.ShouldBe(_storedPayment.Amount.Amount);
        response.Currency.ShouldBe(_storedPayment.Amount.Currency);

        return Task.CompletedTask;
    }
}
