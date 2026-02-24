using MediatR;

using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Features.ProcessPayment;

public sealed record ProcessPaymentCommand(
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    long Amount,
    string Cvv) : IRequest<Result<ProcessPaymentResponse>>;

