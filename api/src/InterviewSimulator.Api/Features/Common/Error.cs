namespace InterviewSimulator.Api.Features.Common;

public record Error
{
    public ErrorType Type { get; }
    public string Code { get; }
    public string Message { get; }

    protected Error(ErrorType type, string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Type = type;
        Code = code;
        Message = message;
    }

    public static Error Unexpected(string code, string message) => new(ErrorType.Unexpected, code, message);
    public static Error Validation(string code, string message) => new(ErrorType.Validation, code, message);
    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
    public static Error Forbidden(string code, string message) => new(ErrorType.Forbidden, code, message);
    public static Error Unauthorized(string code, string message) => new(ErrorType.Unauthorized, code, message);
    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);
    public static Error Concurrency(string code, string message) => new(ErrorType.Concurrency, code, message);
    public static Error RateLimit(string code, string message) => new(ErrorType.RateLimit, code, message);
    public static Error Unavailable(string code, string message) => new(ErrorType.Unavailable, code, message);

    public static Error FromDomain(DomainError domainError, ErrorType type = ErrorType.Unexpected) => new(type, domainError.Code, domainError.Message);

    public static Error FromDomainException(DomainException exception) => exception switch
    {
        DomainRuleViolationException => new(ErrorType.Validation, exception.Code, exception.Message),
        DomainConflictException => new(ErrorType.Conflict, exception.Code, exception.Message),
        DomainNotFoundException => new(ErrorType.NotFound, exception.Code, exception.Message),
        _ => new(ErrorType.Unexpected, exception.Code, exception.Message)
    };
}

public enum ErrorType
{
    Validation = 1,
    Conflict = 2,
    Forbidden = 3,
    Unauthorized = 4,
    NotFound = 5,
    Concurrency = 6,
    RateLimit = 7,
    Unavailable = 8,
    Unexpected = 9
}

public readonly record struct DomainError
{
    public string Code { get; }
    public string Message { get; }

    public DomainError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
    }
}

public sealed record ValidationError : Error
{
    public ValidationError(
        string code,
        string message,
        IReadOnlyList<ValidationErrorDetail> details)
        : base(ErrorType.Validation, code, message)
    {
        Details = [.. details];
    }

    public static ValidationError Create(string code, string message, IReadOnlyList<ValidationErrorDetail> details) =>
        new(code, message, details);

    public IReadOnlyList<ValidationErrorDetail> Details { get; }
}

public sealed record ValidationErrorDetail(string Field, string Message);