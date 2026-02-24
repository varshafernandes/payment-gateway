using System.Reflection;

namespace PaymentGateway.Api.Shared;

public sealed record Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Error = error;
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

public static class ResultFactory
{
    public static TResult CreateFailure<TResult>(Error error)
    {
        var failureMethod = typeof(TResult).GetMethod(
            nameof(Result<object>.Failure),
            BindingFlags.Static | BindingFlags.Public);

        return (TResult)failureMethod!.Invoke(null, [error])!;
    }
}
