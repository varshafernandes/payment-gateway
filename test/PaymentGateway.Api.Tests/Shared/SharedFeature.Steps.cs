using LightBDD.XUnit2;

using PaymentGateway.Api.Shared;

using Shouldly;

namespace PaymentGateway.Api.Tests.Shared;

public partial class SharedFeature : FeatureFixture
{
    private Result<string>? _stringResult;
    private string? _matchOutput;

    private Task When_a_success_result_is_created()
    {
        _stringResult = Result<string>.Success("hello");
        return Task.CompletedTask;
    }

    private Task When_a_failure_result_is_created()
    {
        _stringResult = Result<string>.Failure(Errors.Validation("bad input"));
        return Task.CompletedTask;
    }

    private Task When_a_success_result_is_matched()
    {
        var result = Result<int>.Success(42);
        _matchOutput = result.Match(
            onSuccess: v => $"value={v}",
            onFailure: e => $"error={e.Code}");
        return Task.CompletedTask;
    }

    private Task Then_IsSuccess_is_true()
    {
        _stringResult!.IsSuccess.ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_IsFailure_is_true()
    {
        _stringResult!.IsFailure.ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_the_value_is_accessible()
    {
        _stringResult!.Value.ShouldBe("hello");
        return Task.CompletedTask;
    }

    private Task Then_the_error_is_accessible()
    {
        _stringResult!.Error.ShouldNotBeNull();
        _stringResult.Error!.Code.ShouldBe(ErrorCodes.ValidationFailed);
        return Task.CompletedTask;
    }

    private Task Then_the_success_branch_is_invoked()
    {
        _matchOutput.ShouldBe("value=42");
        return Task.CompletedTask;
    }

    private Task Then_USD_is_supported()
    {
        SupportedCurrencies.IsValid("USD").ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_EUR_is_supported()
    {
        SupportedCurrencies.IsValid("EUR").ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_GBP_is_supported()
    {
        SupportedCurrencies.IsValid("GBP").ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_JPY_is_not_supported()
    {
        SupportedCurrencies.IsValid("JPY").ShouldBeFalse();
        return Task.CompletedTask;
    }

    private Task Then_null_is_not_supported()
    {
        SupportedCurrencies.IsValid(null).ShouldBeFalse();
        return Task.CompletedTask;
    }

    private Task When_money_is_created_with_invalid_currency_then_it_throws()
    {
        Should.Throw<ArgumentException>(() => new Money(100, "JPY"));
        return Task.CompletedTask;
    }

    private Task When_money_is_created_with_zero_amount_then_it_throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Money(0, "GBP"));
        return Task.CompletedTask;
    }
}
