using System.Net;
using System.Text.Json;

using LightBDD.XUnit2;

using Microsoft.Extensions.Logging;

using NSubstitute;

using PaymentGateway.Api.Infrastructure.Bank;
using PaymentGateway.Api.Shared;

using Shouldly;

namespace PaymentGateway.Api.Tests.Infrastructure;

public partial class BankClientFeature : FeatureFixture
{
    private readonly ILogger<BankClient> _logger = Substitute.For<ILogger<BankClient>>();

    private DelegatingHandler _handler = null!;
    private BankClient _bankClient = null!;
    private Result<BankPaymentResponse>? _result;

    private static readonly BankPaymentRequest SampleRequest =
        new("1234567890123456", "06/2026", "GBP", 1000, "123");

    private Task Given_the_bank_throws_a_network_exception()
    {
        _handler = new FakeHandler(_ => throw new HttpRequestException("Connection refused"));
        _bankClient = CreateClient();
        return Task.CompletedTask;
    }

    private Task Given_the_bank_times_out()
    {
        _handler = new FakeHandler(_ =>
            throw new TaskCanceledException("Timeout", new TimeoutException("The operation timed out")));
        _bankClient = CreateClient();
        return Task.CompletedTask;
    }

    private Task Given_the_bank_returns_503()
    {
        _handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        _bankClient = CreateClient();
        return Task.CompletedTask;
    }

    private Task Given_the_bank_returns_400()
    {
        _handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        _bankClient = CreateClient();
        return Task.CompletedTask;
    }

    private Task Given_the_bank_returns_authorised()
    {
        var body = JsonSerializer.Serialize(new { authorized = true, authorization_code = "AUTH-123" });
        _handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        });
        _bankClient = CreateClient();
        return Task.CompletedTask;
    }

    private Task Given_the_bank_returns_500()
    {
        _handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        _bankClient = CreateClient();
        return Task.CompletedTask;
    }

    private async Task When_the_bank_client_processes_a_payment()
    {
        _result = await _bankClient.ProcessPaymentAsync(SampleRequest);
    }

    private Task Then_the_result_is_a_failure()
    {
        _result.ShouldNotBeNull();
        _result!.IsFailure.ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_the_result_is_successful()
    {
        _result.ShouldNotBeNull();
        _result!.IsSuccess.ShouldBeTrue();
        return Task.CompletedTask;
    }

    private Task Then_the_error_code_is_bank_unavailable()
    {
        _result!.Error!.Code.ShouldBe(ErrorCodes.BankUnavailable);
        return Task.CompletedTask;
    }

    private Task Then_the_error_code_is_bank_rejected()
    {
        _result!.Error!.Code.ShouldBe(ErrorCodes.BankRejected);
        return Task.CompletedTask;
    }

    private Task Then_the_error_code_is_internal()
    {
        _result!.Error!.Code.ShouldBe(ErrorCodes.InternalError);
        return Task.CompletedTask;
    }

    private Task Then_the_response_is_authorised_with_code()
    {
        _result!.Value.ShouldNotBeNull();
        _result!.Value!.Authorized.ShouldBeTrue();
        _result!.Value!.AuthorizationCode.ShouldBe("AUTH-123");
        return Task.CompletedTask;
    }

    private BankClient CreateClient()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://bank.test")
        };
        return new BankClient(httpClient, _logger);
    }

    private sealed class FakeHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
        {
            _factory = factory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_factory(request));
        }
    }

    private Task Then_no_log_message_contains_the_full_card_number()
    {
        var fullCardNumber = SampleRequest.CardNumber; 

        var allCalls = _logger.ReceivedCalls();
        foreach (var call in allCalls)
        {
            var args = call.GetArguments();
            var fullText = string.Join(" ", args.Where(a => a is not null).Select(a => a!.ToString()));
            fullText.ShouldNotContain(fullCardNumber);
        }

        return Task.CompletedTask;
    }
}
