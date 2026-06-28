namespace PeopleRise.SharedKernel;

/// <summary>Provider-neutral failure category. The web boundary maps these to HTTP status codes;
/// SharedKernel deliberately knows nothing about HTTP.</summary>
public enum ErrorType { Failure, Validation, NotFound, Conflict, Unauthorized, Forbidden }

public sealed record Error(ErrorType Type, string Message)
{
    public static Error Failure(string m) => new(ErrorType.Failure, m);
    public static Error Validation(string m) => new(ErrorType.Validation, m);
    public static Error NotFound(string m) => new(ErrorType.NotFound, m);
    public static Error Conflict(string m) => new(ErrorType.Conflict, m);
    public static Error Unauthorized(string m) => new(ErrorType.Unauthorized, m);
    public static Error Forbidden(string m) => new(ErrorType.Forbidden, m);
}

/// <summary>Outcome of a use case: success, or a categorised error. Usable by any module.</summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null) throw new InvalidOperationException("A success carries no error.");
        if (!isSuccess && error is null) throw new InvalidOperationException("A failure requires an error.");
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static Result Invalid(string m) => Failure(Error.Validation(m));
    public static Result NotFound(string m) => Failure(Error.NotFound(m));
    public static Result Conflict(string m) => Failure(Error.Conflict(m));

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
}

/// <summary>A result that yields a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No value on a failed result.");

    private Result(T value) : base(true, null) => _value = value;
    private Result(Error error) : base(false, error) => _value = default;

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new(error);

    public static new Result<T> Invalid(string m) => new(Error.Validation(m));
    public static new Result<T> NotFound(string m) => new(Error.NotFound(m));
    public static new Result<T> Conflict(string m) => new(Error.Conflict(m));

    // Ergonomics: `return dto;` or `return Error.NotFound("…");` inside a Result<T>-returning handler.
    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
}
