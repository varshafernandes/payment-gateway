using MediatR;

using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Infrastructure.Bank;
using PaymentGateway.Api.Infrastructure.Repositories;
using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Features.ProcessPayment;

public sealed class ProcessPaymentCommandHandler
    : IRequestHandler<ProcessPaymentCommand, Result<ProcessPaymentResponse>>
{
    private readonly IBankClient _bankClient;
    private readonly IPaymentRepository _repository;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IBankClient bankClient,
        IPaymentRepository repository,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _bankClient = bankClient;
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<ProcessPaymentResponse>> Handle(
        ProcessPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var cardLastFour = command.CardNumber[^4..];

        _logger.LogInformation(
            "Processing payment — card ending {CardLastFour}, {Currency} {Amount}",
            cardLastFour, command.Currency, command.Amount);

        var bankRequest = new BankPaymentRequest(
            command.CardNumber,
            $"{command.ExpiryMonth:D2}/{command.ExpiryYear}",
            command.Currency,
            command.Amount,
            command.Cvv);

        var bankResult = await _bankClient.ProcessPaymentAsync(bankRequest, cancellationToken);

        if (bankResult.IsFailure)
        {
            _logger.LogWarning(
                "Bank returned error for card ending {CardLastFour}: {ErrorCode}",
                cardLastFour, bankResult.Error!.Code);
            return bankResult.Error;
        }

        var bankResponse = bankResult.Value!;
        var status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
        var amount = new Money(command.Amount, command.Currency);

        var payment = Payment.Create(
            cardLastFour,
            command.ExpiryMonth,
            command.ExpiryYear,
            amount,
            status,
            bankResponse.AuthorizationCode);

        _repository.Add(payment);

        _logger.LogInformation(
            "Payment {PaymentId} completed — {Status}, card ending {CardLastFour}",
            payment.Id, payment.Status, cardLastFour);

        return ProcessPaymentResponse.FromPayment(payment);
    }
}

