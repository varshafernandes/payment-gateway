using FluentValidation.Results;

using LightBDD.XUnit2;

using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Features.ProcessPayment;
using PaymentGateway.Api.Tests.Utilities;

using Shouldly;

namespace PaymentGateway.Api.Tests.Features;

public partial class ValidationFeature : FeatureFixture
{
    private static readonly FakeTimeProvider Clock =
        new(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));

    private readonly ProcessPaymentCommandValidator _validator = new(Clock);
    private ProcessPaymentCommand _command = null!;
    private ValidationResult _validationResult = new();

    private Task Given_a_valid_command()
    {
        _command = PaymentCommandBuilder.Valid(Clock).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_expired_card()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithExpiryMonth(1).WithExpiryYear(2020).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_current_year_and_past_month()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithExpiryMonth(5).WithExpiryYear(2025).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_current_year_and_current_month()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithExpiryMonth(6).WithExpiryYear(2025).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_future_year_and_january()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithExpiryMonth(1).WithExpiryYear(2026).Build();
        return Task.CompletedTask;
    }

    private Task Given_a_command_with_past_year()
    {
        _command = PaymentCommandBuilder.Valid(Clock).WithExpiryMonth(12).WithExpiryYear(2024).Build();
        return Task.CompletedTask;
    }

    private async Task When_the_command_is_validated()
    {
        _validationResult = await _validator.ValidateAsync(_command);
    }

    private Task Then_the_result_is_valid()
    {
        _validationResult.IsValid.ShouldBeTrue(
            $"Expected valid but got errors: {string.Join("; ", _validationResult.Errors.Select(e => e.ErrorMessage))}");
        return Task.CompletedTask;
    }

    private Task Then_the_result_is_invalid()
    {
        _validationResult.IsValid.ShouldBeFalse("Expected validation to fail but it passed.");
        return Task.CompletedTask;
    }

    private Task Then_the_error_says_card_has_expired()
        => AssertErrorMessage("Card has expired.");

    private Task Then_the_error_says_expiry_year_must_not_be_in_the_past()
        => AssertErrorMessage("Expiry year must not be in the past.");

    private Task Then_the_error_is_on_ExpiryYear_field()
        => AssertErrorOnField("ExpiryYear");

    private Task AssertErrorMessage(string expectedMessage)
    {
        _validationResult.Errors
            .ShouldContain(e => e.ErrorMessage == expectedMessage,
                $"Expected error '{expectedMessage}' but got: {string.Join("; ", _validationResult.Errors.Select(e => e.ErrorMessage))}");
        return Task.CompletedTask;
    }

    private Task AssertErrorOnField(string fieldName)
    {
        _validationResult.Errors
            .ShouldContain(e => e.PropertyName == fieldName,
                $"Expected error on field '{fieldName}' but errors were on: {string.Join(", ", _validationResult.Errors.Select(e => e.PropertyName))}");
        return Task.CompletedTask;
    }
}
