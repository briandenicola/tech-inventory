namespace TechInventory.Application.Common.Results;

public record Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("Successful results cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("Failed results must carry an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));
}

public sealed record Result<T> : Result
{
    private Result(bool isSuccess, T? value, Error? error)
        : base(isSuccess, error)
    {
        if (isSuccess && value is null)
        {
            throw new ArgumentNullException(nameof(value), "Successful generic results must carry a value.");
        }

        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null);

    public new static Result<T> Failure(Error error) => new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
}
