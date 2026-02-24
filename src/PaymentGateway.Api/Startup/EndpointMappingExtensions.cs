using MediatR;

using PaymentGateway.Api.Features.GetPayment;
using PaymentGateway.Api.Features.ProcessPayment;
using PaymentGateway.Api.Shared;

namespace PaymentGateway.Api.Startup;

public static class EndpointMappingExtensions
{
    public static WebApplication MapPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/payments", async (ProcessPaymentRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(request.ToCommand());

            return result.Match<IResult>(
                onSuccess: response => Results.Ok(response),
                onFailure: error => MapError(error));
        })
        .WithName("ProcessPayment")
        .Produces<ProcessPaymentResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status502BadGateway);

        app.MapGet("/api/payments/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPaymentQuery(id));

            return result.Match<IResult>(
                onSuccess: response => Results.Ok(response),
                onFailure: error => MapError(error));
        })
        .WithName("GetPayment")
        .Produces<GetPaymentResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }

    private static IResult MapError(Error error) => error.Code switch
    {
        ErrorCodes.ValidationFailed => Results.BadRequest(ErrorResponse.FromError(error)),
        ErrorCodes.PaymentNotFound  => Results.NotFound(ErrorResponse.FromError(error)),
        ErrorCodes.BankUnavailable  => Results.StatusCode(StatusCodes.Status502BadGateway),
        ErrorCodes.BankRejected     => Results.BadRequest(ErrorResponse.FromError(error)),
        _                           => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
}
