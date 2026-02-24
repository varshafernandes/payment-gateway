using FluentValidation;

using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Features.ProcessPayment;

public sealed class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    private readonly TimeProvider _timeProvider;

    public ProcessPaymentCommandValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        RuleFor(x => x.CardNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Card number is required.")
            .Length(14, 19)
                .WithMessage("Card number must be between 14 and 19 digits.")
            .Must(BeNumericOnly)
                .WithMessage("Card number must contain digits only.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12)
                .WithMessage("Expiry month must be between 1 and 12.");

        RuleFor(x => x.ExpiryYear)
            .Must(NotBeInThePast)
                .WithMessage("Expiry year must not be in the past.");

        RuleFor(x => x)
            .Must(NotBeExpired)
                .WithMessage("Card has expired.")
                .When(x => x.ExpiryMonth is >= 1 and <= 12);

        RuleFor(x => x.Currency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Currency is required.")
            .Length(3)
                .WithMessage("Currency must be a 3-letter ISO code.")
            .Must(BeSupportedCurrency)
                .WithMessage(x => $"Currency '{x.Currency}' is not supported. Supported currencies are {string.Join(", ", SupportedCurrencies.All)}.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Cvv)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("CVV is required.")
            .Length(3, 4)
                .WithMessage("CVV must be 3 or 4 digits.")
            .Must(BeNumericOnly)
                .WithMessage("CVV must contain digits only.");
    }

    private static bool BeNumericOnly(string? value) =>
        !string.IsNullOrEmpty(value) && value.All(char.IsDigit);

    private static bool BeSupportedCurrency(string? currency) =>
        SupportedCurrencies.IsValid(currency);

    private bool NotBeInThePast(int expiryYear) =>
        expiryYear >= _timeProvider.GetUtcNow().Year;

    private bool NotBeExpired(ProcessPaymentCommand command)
    {
        try
        {
            var lastDayOfExpiryMonth = new DateTime(command.ExpiryYear, command.ExpiryMonth, 1)
                .AddMonths(1)
                .AddDays(-1);

            return lastDayOfExpiryMonth >= _timeProvider.GetUtcNow().DateTime.Date;
        }
        catch
        {
            return false;
        }
    }
}

