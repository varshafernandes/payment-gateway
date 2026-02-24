using Bogus;

using PaymentGateway.Api.Features.ProcessPayment;
using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Tests.Utilities;

public sealed class PaymentFaker
{
    private readonly Faker _faker;

    public PaymentFaker(int? seed = null)
    {
        _faker = seed.HasValue ? new Faker { Random = new Randomizer(seed.Value) } : new Faker();
    }

    public string CardNumber() => string.Concat(_faker.Random.Digits(16));

    public string CardLastFour() => string.Concat(_faker.Random.Digits(4));

    public (int Month, int Year) FutureExpiry()
    {
        var month = _faker.Random.Int(1, 12);
        var year = DateTime.UtcNow.Year + _faker.Random.Int(1, 5);
        return (month, year);
    }

    public string Currency() => _faker.PickRandom("USD", "EUR", "GBP");

    public long Amount() => _faker.Random.Long(1, 999_999);

    public string Cvv()
    {
        var length = _faker.Random.Int(3, 4);
        return string.Concat(_faker.Random.Digits(length));
    }

    public ProcessPaymentCommand ValidCommand()
    {
        var (month, year) = FutureExpiry();
        return new ProcessPaymentCommand(CardNumber(), month, year, Currency(), Amount(), Cvv());
    }

    public Money ValidMoney() => new(Amount(), Currency());

    public string AuthorisationCode() => _faker.Random.AlphaNumeric(8);
}
