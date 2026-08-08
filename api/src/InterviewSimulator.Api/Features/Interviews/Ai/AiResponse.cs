namespace InterviewSimulator.Api.Features.Interviews.Ai;

public sealed record AiCallMetadata(
    string PromptVersion,
    string Provider,
    string? Model,
    int? PromptTokens,
    int? CompletionTokens);

public sealed record AiResponse<TResponse>(
    TResponse Value,
    AiCallMetadata Metadata);

public sealed record AiRawResponse(
    string Content,
    AiCallMetadata Metadata);

public sealed record AiOperationContext(
    string OperationName,
    string PromptVersion,
    string Provider,
    string? Model);