using InterviewSimulator.Api.Features.Common;

namespace InterviewSimulator.Api.Features.Interviews.Ai;

public abstract class AiException(
    string code,
    AiOperationContext context,
    string message,
    Exception? innerException = null)
    : InfrastructureException(Error.Unavailable(code, message), innerException)
{
    public AiOperationContext Context { get; } = context;

    public string OperationName => Context.OperationName;

    public string PromptVersion => Context.PromptVersion;
}

public sealed class AiInvalidResponseException(
    AiOperationContext context,
    string reason,
    IReadOnlyList<string>? validationErrors = null,
    Exception? innerException = null)
    : AiException(
        code: "Interviews.Ai.InvalidResponse",
        context,
        message: "The AI service returned an invalid response. Please retry.",
        innerException)
{
    public string Reason { get; } = reason;

    public IReadOnlyList<string> ValidationErrors { get; } = validationErrors ?? [];
}

// Thrown by Azure adapters for transient provider failures; caught and retried by AiStructuredOutputRunner.
public sealed class AiProviderTransientException(
    AiOperationContext context,
    string reason,
    Exception? innerException = null)
    : AiException(
        code: "Interviews.Ai.ProviderTransient",
        context,
        message: "The AI service could not complete the request. Please retry.",
        innerException)
{
    public string Reason { get; } = reason;
}

public sealed class AiProviderUnavailableException(
    AiOperationContext context,
    string reason,
    Exception? innerException = null)
    : AiException(
        code: "Interviews.Ai.ProviderUnavailable",
        context,
        message: "The AI service could not complete the request. Please retry.",
        innerException)
{
    public string Reason { get; } = reason;
}