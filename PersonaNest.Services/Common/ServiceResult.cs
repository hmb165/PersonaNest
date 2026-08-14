namespace PersonaNest.Services.Common;

/// <summary>
/// The outcome of a service operation. Services return this instead of throwing for expected
/// business failures - "you already logged this media", "an application is already pending" -
/// so controllers can turn a failure into a validation message rather than a 500.
/// Genuine faults (a dropped connection) still throw.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; protected init; }

    public IReadOnlyList<string> Errors { get; protected init; } = Array.Empty<string>();

    public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

    public static ServiceResult Success() => new() { Succeeded = true };

    public static ServiceResult Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}

/// <summary>A <see cref="ServiceResult"/> that carries a value on success.</summary>
public sealed class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    public static ServiceResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static new ServiceResult<T> Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}
