using System.Net;

using InterviewSimulator.Api.Features.Common;

using Microsoft.Azure.Cosmos;

namespace InterviewSimulator.Api.Infrastructure.Data;

internal static class CosmosFailureTranslator
{
    public static void ThrowIfFailure(
        HttpStatusCode statusCode,
        string resourceName,
        string operation,
        bool treatNotFoundAsConflict = false)
    {
        if ((int)statusCode is >= 200 and < 300)
        {
            return;
        }

        throw CreateException(statusCode, resourceName, operation, treatNotFoundAsConflict);
    }

    public static void ThrowIfFailure(
        TransactionalBatchResponse response,
        string resourceName,
        string operation,
        bool treatNotFoundAsConflict = false)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var statusCode = response.StatusCode;

        foreach (var operationResult in response)
        {
            if (!operationResult.IsSuccessStatusCode)
            {
                statusCode = operationResult.StatusCode;
                break;
            }
        }

        throw CreateException(statusCode, resourceName, operation, treatNotFoundAsConflict);
    }

    public static InfrastructureException ToException(
        CosmosException exception,
        string resourceName,
        string operation,
        bool treatNotFoundAsConflict = false)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return CreateException(exception.StatusCode, resourceName, operation, treatNotFoundAsConflict, exception);
    }

    private static InfrastructureException CreateException(
        HttpStatusCode statusCode,
        string resourceName,
        string operation,
        bool treatNotFoundAsConflict,
        Exception? innerException = null)
    {
        var codePrefix = $"Infrastructure.Cosmos.{resourceName}.{operation}";
        var resourceDescription = resourceName switch
        {
            "Interviews" => "Interview persistence",
            "UserProfiles" => "User profile persistence",
            _ => "Cosmos DB persistence"
        };

        if (treatNotFoundAsConflict && statusCode == HttpStatusCode.NotFound)
        {
            return new InfrastructureConflictException(
                Error.Concurrency(
                    $"{codePrefix}.Conflict",
                    $"{resourceDescription} detected a concurrent update."),
                innerException);
        }

        return statusCode switch
        {
            HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed => new InfrastructureConflictException(
                Error.Concurrency(
                    $"{codePrefix}.Conflict",
                    $"{resourceDescription} detected a concurrent update."),
                innerException),

            HttpStatusCode.RequestTimeout or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout or (HttpStatusCode)429 => new InfrastructureUnavailableException(
                Error.Unavailable(
                    $"{codePrefix}.Unavailable",
                    $"{resourceDescription} is temporarily unavailable."),
                innerException),

            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new InfrastructureUnexpectedException(
                Error.Unexpected(
                    $"{codePrefix}.Misconfigured",
                    $"{resourceDescription} is misconfigured."),
                innerException),

            _ => new InfrastructureUnexpectedException(
                Error.Unexpected(
                    $"{codePrefix}.Failed",
                    $"{resourceDescription} failed unexpectedly."),
                innerException)
        };
    }
}