using System.Diagnostics.CodeAnalysis;

namespace AnalyticsPlatform.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string[] Errors { get; }

    protected Result(bool isSuccess, string[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(params string[] errors) => Result<T>.Failure(errors);
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string[] errors) : base(isSuccess, errors)
        => Value = value;

    public static Result<T> Success(T value) => new(true, value, Array.Empty<string>());
    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);
}
