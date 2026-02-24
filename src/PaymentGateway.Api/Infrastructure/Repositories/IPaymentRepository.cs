using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Infrastructure.Repositories;

public interface IPaymentRepository
{
    void Add(Payment payment);
    Payment? GetById(Guid id);
}
