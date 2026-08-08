using System.Text.Json;

using FluentValidation;
using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Interviews.Ai;

public sealed class AiStructuredOutputRunner<TOutput>(
    IOptions<AiOptions> aiOptions,
    IValidator<TOutput> validator,
    ILogger<AiStructuredOutputRunner<TOutput>> logger)
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiResponse<TOutput>> RunAsync(
        AiOperationContext context,
        Func<CancellationToken, Task<AiRawResponse>> aiCall,
        CancellationToken cancellationToken)
    {
        var invalidOutputFailures = 0;
        var transientFailures = 0;
        var configuredOptions = aiOptions.Value;
        var maxAttempts =
            1 + configuredOptions.InvalidOutputRetryCount + configuredOptions.TransientRetryCount;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var rawResponse = await aiCall(cancellationToken);

                if (string.IsNullOrWhiteSpace(rawResponse.Content))
                {
                    invalidOutputFailures++;

                    if (invalidOutputFailures > configuredOptions.InvalidOutputRetryCount)
                    {
                        throw new AiInvalidResponseException(
                            context,
                            "AI response content was empty.");
                    }

                    logger.LogWarning(
                        "AI returned empty content for {OperationName}. Attempt {Attempt} of {MaxAttempts}.",
                        context.OperationName,
                        attempt,
                        maxAttempts);

                    continue;
                }

                TOutput? parsed;

                try
                {
                    parsed = JsonSerializer.Deserialize<TOutput>(
                        rawResponse.Content,
                        _jsonOptions);
                }
                catch (JsonException ex)
                {
                    invalidOutputFailures++;

                    if (invalidOutputFailures > configuredOptions.InvalidOutputRetryCount)
                    {
                        throw new AiInvalidResponseException(
                            context,
                            "AI response was not valid JSON.",
                            innerException: ex);
                    }

                    logger.LogWarning(
                        ex,
                        "AI returned malformed JSON for {OperationName}. Attempt {Attempt} of {MaxAttempts}.",
                        context.OperationName,
                        attempt,
                        maxAttempts);

                    continue;
                }

                if (parsed is null)
                {
                    invalidOutputFailures++;

                    if (invalidOutputFailures > configuredOptions.InvalidOutputRetryCount)
                    {
                        throw new AiInvalidResponseException(
                            context,
                            "AI response could not be deserialized.");
                    }

                    logger.LogWarning(
                        "AI response deserialized to null for {OperationName}. Attempt {Attempt} of {MaxAttempts}.",
                        context.OperationName,
                        attempt,
                        maxAttempts);

                    continue;
                }

                var validationResult = await validator.ValidateAsync(
                    parsed,
                    cancellationToken);

                if (!validationResult.IsValid)
                {
                    invalidOutputFailures++;

                    if (invalidOutputFailures > configuredOptions.InvalidOutputRetryCount)
                    {
                        throw new AiInvalidResponseException(
                            context,
                            "AI response failed validation.",
                            [.. validationResult.Errors.Select(e => e.ErrorMessage)]);
                    }

                    logger.LogWarning(
                        "AI response validation failed for {OperationName}. Attempt {Attempt} of {MaxAttempts}. Errors: {Errors}",
                        context.OperationName,
                        attempt,
                        maxAttempts,
                        validationResult.Errors.Select(e => e.ErrorMessage));

                    continue;
                }

                return new AiResponse<TOutput>(parsed, rawResponse.Metadata);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AiProviderTransientException ex)
            {
                transientFailures++;

                if (transientFailures > configuredOptions.TransientRetryCount)
                {
                    throw new AiProviderUnavailableException(
                        context,
                        "AI provider was unavailable after retry attempts.",
                        ex);
                }

                logger.LogWarning(
                    ex,
                    "Transient AI provider failure for {OperationName}. Attempt {Attempt} of {MaxAttempts}.",
                    context.OperationName,
                    attempt,
                    maxAttempts);
            }
        }

        throw new AiProviderUnavailableException(
            context,
            "AI operation failed after retry attempts.");
    }
}
