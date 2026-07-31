using Microsoft.AspNetCore.Diagnostics;

namespace InterviewSimulator.Api.Features.Common;

public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        var error = Error.FromDomainException(domainException);
        var logLevel = error.Type switch
        {
            ErrorType.Validation => LogLevel.Information,
            ErrorType.Conflict => LogLevel.Information,
            ErrorType.NotFound => LogLevel.Information,
            _ => LogLevel.Warning
        };

        logger.Log(
            logLevel,
            exception,
            "Mapped domain exception to ProblemDetails response (code: {ErrorCode}, type: {ErrorType}).",
            domainException.Code,
            error.Type);

        await error
            .ToProblemResult()
            .ExecuteAsync(httpContext);

        return true;
    }
}