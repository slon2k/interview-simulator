using Azure;
using Azure.AI.OpenAI;

using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.Options;

using Microsoft.Extensions.Options;

using OpenAI.Chat;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed class AzureOpenAISessionSummarizer(
    AiStructuredOutputRunner<SessionSummaryResponse> runner,
    AzureOpenAIClient aiClient,
    IOptions<AiOptions> aiOptions,
    IOptions<AzureOpenAIOptions> openAiOptions) : ISessionSummarizer
{
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private readonly AzureOpenAIOptions _openAiOptions = openAiOptions.Value;

    private const string _provider = AiProviders.AzureOpenAI;
    private const string _operation = "SessionSummary";

    public async Task<SessionSummaryResult> GenerateSummaryAsync(
        SessionSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Turns);

        var deploymentName = ResolveDeploymentName();
        var prompt = PromptBuilder.BuildSessionSummaryPrompt(
            targetRole: request.TargetRole,
            seniority: request.Seniority,
            interviewType: request.InterviewType,
            focusArea: request.FocusArea,
            turns: request.Turns,
            options: _aiOptions);

        var chatClient = aiClient.GetChatClient(deploymentName);
        var completionOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };
        var operationContext = new AiOperationContext(
            OperationName: _operation,
            PromptVersion: PromptVersions.SessionSummary,
            Provider: _provider,
            Model: deploymentName);

        var response = await runner.RunAsync(
            context: operationContext,
            aiCall: async ct =>
            {
                try
                {
                    var completion = await chatClient.CompleteChatAsync(
                        [new UserChatMessage(prompt)],
                        completionOptions,
                        cancellationToken: ct);

                    var content = completion.Value.Content.Count > 0
                        ? completion.Value.Content[0].Text
                        : string.Empty;

                    var usage = completion.Value.Usage;

                    return new AiRawResponse(
                        Content: content,
                        Metadata: new AiCallMetadata(
                            PromptVersion: PromptVersions.SessionSummary,
                            Provider: _provider,
                            Model: deploymentName,
                            PromptTokens: usage?.InputTokenCount,
                            CompletionTokens: usage?.OutputTokenCount));
                }
                catch (RequestFailedException ex) when (IsTransient(ex.Status))
                {
                    throw new AiProviderTransientException(operationContext, $"Transient failure from {_provider}.", ex);
                }
                catch (RequestFailedException ex)
                {
                    throw new AiProviderUnavailableException(operationContext, $"{_provider} request failed.", ex);
                }
            },
            cancellationToken: cancellationToken);

        return new SessionSummaryResult(
            Summary: response.Value.Summary ?? throw new AiInvalidResponseException(operationContext, "AI response summary was null."),
            AiMetadata: response.Metadata);
    }

    private string ResolveDeploymentName()
    {
        if (!string.IsNullOrWhiteSpace(_openAiOptions.DefaultDeploymentName))
        {
            return _openAiOptions.DefaultDeploymentName;
        }

        var fallback = _openAiOptions.DeploymentNames.FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        throw new InvalidOperationException("Azure OpenAI deployment is not configured.");
    }

    private static bool IsTransient(int statusCode) =>
        statusCode is 408 or 429 or 500 or 502 or 503 or 504;
}
