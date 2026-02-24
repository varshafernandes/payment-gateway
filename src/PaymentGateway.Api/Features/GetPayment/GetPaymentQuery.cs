using MediatR;

using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Features.GetPayment;

/// <summary>
/// CQRS query to retrieve a previously processed payment by its identifier.
/// </summary>
public sealed record GetPaymentQuery(Guid PaymentId) : IRequest<Result<GetPaymentResponse>>;

