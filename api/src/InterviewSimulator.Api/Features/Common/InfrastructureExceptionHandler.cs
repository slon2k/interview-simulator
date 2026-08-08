using InterviewSimulator.Api.Features.Interviews.Ai;
using Microsoft.AspNetCore.Diagnostics;

namespace InterviewSimulator.Api.Features.Common;

public sealed class InfrastructureExceptionHandler(ILogger<InfrastructureExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is AiException aiException)
        {
            logger.LogWarning(
                exception,
                "AI operation failed (code: {ErrorCode}, operation: {OperationName}).",
                aiException.Code,
                aiException.OperationName);

            await Error.Unavailable(aiException.Code, aiException.Message)
                .ToProblemResult()
                .ExecuteAsync(httpContext);

            return true;
        }

        if (exception is not InfrastructureException infrastructureException)
        {
            return false;
        }

        logger.LogError(
            exception,
            "Mapped infrastructure exception to ProblemDetails response (code: {ErrorCode}).",
            infrastructureException.Code);

        var error = infrastructureException.Type switch
        {
            ErrorType.Validation => Error.Validation(infrastructureException.Code, infrastructureException.Message),
            ErrorType.Conflict => Error.Conflict(infrastructureException.Code, infrastructureException.Message),
            ErrorType.Forbidden => Error.Forbidden(infrastructureException.Code, infrastructureException.Message),
            ErrorType.Unauthorized => Error.Unauthorized(infrastructureException.Code, infrastructureException.Message),
            ErrorType.NotFound => Error.NotFound(infrastructureException.Code, infrastructureException.Message),
            ErrorType.Concurrency => Error.Concurrency(infrastructureException.Code, infrastructureException.Message),
            ErrorType.RateLimit => Error.RateLimit(infrastructureException.Code, infrastructureException.Message),
            ErrorType.Unavailable => Error.Unavailable(infrastructureException.Code, infrastructureException.Message),
            _ => Error.Unexpected(infrastructureException.Code, infrastructureException.Message)
        };

        await error
            .ToProblemResult()
            .ExecuteAsync(httpContext);

        return true;
    }
}