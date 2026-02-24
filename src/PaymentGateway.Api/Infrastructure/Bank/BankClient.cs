using System.Net;
using System.Net.Http.Json;

using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Infrastructure.Bank;

public sealed class BankClient : IBankClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BankClient> _logger;

    public BankClient(HttpClient httpClient, ILogger<BankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<BankPaymentResponse>> ProcessPaymentAsync(
        BankPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var cardLastFour = request.CardNumber.Length >= 4
            ? request.CardNumber[^4..]
            : "????";

        _logger.LogInformation(
            "Sending payment to bank — card ending {CardLastFour}, {Currency} {Amount}",
            cardLastFour, request.Currency, request.Amount);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/payments", request, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => await DeserialiseSuccessAsync(response, cardLastFour, cancellationToken),
                HttpStatusCode.ServiceUnavailable => HandleServiceUnavailable(cardLastFour),
                HttpStatusCode.BadRequest => HandleBadRequest(cardLastFour),
                _ => HandleUnexpectedStatus(response.StatusCode, cardLastFour)
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error communicating with bank");
            return Errors.BankUnavailable();
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout communicating with bank");
            return Errors.BankUnavailable();
        }
    }

    private async Task<Result<BankPaymentResponse>> DeserialiseSuccessAsync(
        HttpResponseMessage response, string cardLastFour, CancellationToken ct)
    {
        var bankResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(cancellationToken: ct);

        if (bankResponse is null)
        {
            _logger.LogError("Bank returned empty body for card ending {CardLastFour}", cardLastFour);
            return Errors.Internal("Bank returned an empty response.");
        }

        _logger.LogInformation(
            "Bank response — {Outcome} for card ending {CardLastFour}",
            bankResponse.Authorized ? "Authorised" : "Declined",
            cardLastFour);

        return bankResponse;
    }

    private Result<BankPaymentResponse> HandleServiceUnavailable(string cardLastFour)
    {
        _logger.LogWarning("Bank returned 503 for card ending {CardLastFour}", cardLastFour);
        return Errors.BankUnavailable();
    }

    private Result<BankPaymentResponse> HandleBadRequest(string cardLastFour)
    {
        _logger.LogWarning("Bank returned 400 for card ending {CardLastFour}", cardLastFour);
        return Errors.BankRejected("Payment was rejected due to invalid request.");
    }

    private Result<BankPaymentResponse> HandleUnexpectedStatus(HttpStatusCode statusCode, string cardLastFour)
    {
        _logger.LogError("Unexpected bank status {StatusCode} for card ending {CardLastFour}", statusCode, cardLastFour);
        return Errors.Internal($"Unexpected bank response: {statusCode}");
    }
}
