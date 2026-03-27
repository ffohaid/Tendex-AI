namespace TendexAI.Domain.Common;

/// <summary>
/// Represents the outcome of an operation, encapsulating success or failure with error details.
/// Used across the application to avoid throwing exceptions for expected failures.
/// </summary>
public record Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot have an error message.");

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("A failed result must have an error message.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);
    public static Result<TValue> Failure<TValue>(string error) => new(default, false, error);
}

/// <summary>
/// Generic result type that carries a value on success.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public record Result<TValue> : Result
{
    public TValue? Value { get; }

    internal Result(TValue? value, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        Value = value;
    }
}
