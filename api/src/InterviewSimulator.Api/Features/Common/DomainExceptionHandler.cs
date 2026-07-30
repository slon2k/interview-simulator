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

        logger.LogWarning(
            exception,
            "Mapped domain exception to ProblemDetails response (code: {ErrorCode}).",
            domainException.Code);

        await Error.FromDomainException(domainException)
            .ToProblemResult()
            .ExecuteAsync(httpContext);

        return true;
    }
}