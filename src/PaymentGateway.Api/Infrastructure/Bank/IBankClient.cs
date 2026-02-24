using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Infrastructure.Bank;

public interface IBankClient
{
    Task<Result<BankPaymentResponse>> ProcessPaymentAsync(BankPaymentRequest request, CancellationToken cancellationToken = default);
}
