using MediatR;

using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Features.GetPayment;

public sealed record GetPaymentQuery(Guid PaymentId) : IRequest<Result<GetPaymentResponse>>;

