using System.Collections.Concurrent;

using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Infrastructure.Repositories;

public sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public void Add(Payment payment) => _payments.TryAdd(payment.Id, payment);

    public Payment? GetById(Guid id) => _payments.GetValueOrDefault(id);
}
