using MediatR;

using PaymentGateway.Api.Infrastructure.Repositories;
using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Features.GetPayment;

/// <summary>
/// Handles <see cref="GetPaymentQuery"/> by looking up a payment from the repository.
/// </summary>
public sealed class GetPaymentQueryHandler
    : IRequestHandler<GetPaymentQuery, Result<GetPaymentResponse>>
{
    private readonly IPaymentRepository _repository;
    private readonly ILogger<GetPaymentQueryHandler> _logger;

    public GetPaymentQueryHandler(
        IPaymentRepository repository,
        ILogger<GetPaymentQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<Result<GetPaymentResponse>> Handle(
        GetPaymentQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving payment {PaymentId}", query.PaymentId);

        var payment = _repository.GetById(query.PaymentId);

        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} not found", query.PaymentId);
            return Task.FromResult<Result<GetPaymentResponse>>(
                Errors.PaymentNotFound(query.PaymentId));
        }

        _logger.LogInformation(
            "Payment {PaymentId} retrieved — Status={Status}",
            query.PaymentId, payment.Status);

        return Task.FromResult<Result<GetPaymentResponse>>(
            GetPaymentResponse.FromPayment(payment));
    }
}

