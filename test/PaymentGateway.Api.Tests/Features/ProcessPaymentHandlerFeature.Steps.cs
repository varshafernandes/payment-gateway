using LightBDD.XUnit2;

using Microsoft.Extensions.Logging;

using NSubstitute;

using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Features.ProcessPayment;
using PaymentGateway.Api.Infrastructure.Bank;
using PaymentGateway.Api.Infrastructure.Repositories;
using PaymentGateway.Api.Shared;
using PaymentGateway.Api.Tests.Utilities;

using Shouldly;

namespace PaymentGateway.Api.Tests.Features;

public partial class ProcessPaymentHandlerFeature : FeatureFixture
{
    private const string SimulatedRepositoryFailure = "Simulated repository failure";

    private readonly PaymentFaker _faker = new();
    private readonly IBankClient _bankClient = Substitute.For<IBankClient>();
    private readonly IPaymentRepository _repository = Substitute.For<IPaymentRepository>();
    private readonly ILogger<ProcessPaymentCommandHandler> _logger =
        Substitute.For<ILogger<ProcessPaymentCommandHandler>>();

    private ProcessPaymentCommand _command = null!;
    private Result<ProcessPaymentResponse>? _result;

    private Task Given_a_valid_command()
    {
        _command = _faker.ValidCommand();
        return Task.CompletedTask;
    }

    private Task Given_the_bank_authorises_the_payment()
    {
        var bankResponse = new BankPaymentResponse(Authorized: true, AuthorizationCode: "auth-code-123");

        _bankClient
            .ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<BankPaymentResponse>.Success(bankResponse));

        return Task.CompletedTask;
    }

    private Task Given_the_bank_declines_the_payment()
    {
        var bankResponse = new BankPaymentResponse(Authorized: false, AuthorizationCode: null);

        _bankClient
            .ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<BankPaymentResponse>.Success(bankResponse));

        return Task.CompletedTask;
    }

    private Task Given_the_bank_is_unavailable()
    {
        _bankClient
            .ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<BankPaymentResponse>.Failure(Errors.BankUnavailable()));

        return Task.CompletedTask;
    }

    private Task Given_the_bank_rejects_the_payment()
    {
        _bankClient
            .ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<BankPaymentResponse>.Failure(Errors.BankRejected("Payment was rejected due to invalid request.")));

        return Task.CompletedTask;
    }

    private Task Given_the_repository_throws_on_add()
    {
        _repository.When(r => r.Add(Arg.Any<Payment>()))
            .Do(_ => throw new InvalidOperationException(SimulatedRepositoryFailure));
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_single_digit_expiry_month()
    {
        var (_, year) = _faker.FutureExpiry();
        _command = new ProcessPaymentCommand(_faker.CardNumber(), 4, year, _faker.Currency(), _faker.Amount(), _faker.Cvv());
        return Task.CompletedTask;
    }

    private async Task When_the_handler_is_invoked()
    {
        var handler = new ProcessPaymentCommandHandler(_bankClient, _repository, _logger);
        _result = await handler.Handle(_command, CancellationToken.None);
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

    private Task Then_the_response_status_is_Authorized()
    {
        _result!.Value!.Status.ShouldBe(PaymentStatus.Authorized.ToString());
        return Task.CompletedTask;
    }

    private Task Then_the_response_status_is_Declined()
    {
        _result!.Value!.Status.ShouldBe(PaymentStatus.Declined.ToString());
        return Task.CompletedTask;
    }

    private Task Then_the_error_code_is_bank_unavailable()
    {
        _result!.Error!.Code.ShouldBe(ErrorCodes.BankUnavailable);
        return Task.CompletedTask;
    }

    private Task Then_the_error_code_is_bank_rejected()
    {
        _result!.Error!.Code.ShouldBe(ErrorCodes.BankRejected);
        return Task.CompletedTask;
    }

    private Task Then_the_payment_is_persisted()
    {
        _repository.Received(1).Add(Arg.Any<Payment>());
        return Task.CompletedTask;
    }

    private Task Then_no_payment_is_persisted()
    {
        _repository.DidNotReceive().Add(Arg.Any<Payment>());
        return Task.CompletedTask;
    }

    private Task Then_the_response_card_last_four_is_correct()
    {
        _result!.Value!.CardNumberLastFour.ShouldBe(_command.CardNumber![^4..]);
        return Task.CompletedTask;
    }

    private Task Then_the_response_amount_matches_the_command()
    {
        _result!.Value!.Amount.ShouldBe(_command.Amount!.Value);
        return Task.CompletedTask;
    }

    private Task Then_the_response_currency_matches_the_command()
    {
        _result!.Value!.Currency.ShouldBe(_command.Currency);
        return Task.CompletedTask;
    }

    private Exception? _caughtException;

    private async Task When_the_handler_is_invoked_expecting_failure()
    {
        var handler = new ProcessPaymentCommandHandler(_bankClient, _repository, _logger);
        _caughtException = await Should.ThrowAsync<Exception>(
            () => handler.Handle(_command, CancellationToken.None));
    }

    private Task Then_the_exception_is_an_InvalidOperationException()
    {
        _caughtException.ShouldBeOfType<InvalidOperationException>();
        _caughtException!.Message.ShouldContain(SimulatedRepositoryFailure);
        return Task.CompletedTask;
    }

    private Task Then_the_response_has_a_non_empty_payment_id()
    {
        _result!.Value!.Id.ShouldNotBe(Guid.Empty);
        return Task.CompletedTask;
    }

    private Task Then_the_bank_received_zero_padded_expiry()
    {
        _bankClient.Received(1).ProcessPaymentAsync(
            Arg.Is<BankPaymentRequest>(r => r.ExpiryDate.StartsWith("04/")),
            Arg.Any<CancellationToken>());
        return Task.CompletedTask;
    }
}
