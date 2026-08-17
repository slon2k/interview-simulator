using Azure;
using Azure.AI.OpenAI;

using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.Options;

using Microsoft.Extensions.Options;

using OpenAI.Chat;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed class AzureOpenAIQuestionGenerator(
    AiStructuredOutputRunner<QuestionGenerationResponse> runner,
    AzureOpenAIClient aiClient,
    IOptions<AiOptions> aiOptions,
    IOptions<AzureOpenAIOptions> openAiOptions) : IQuestionGenerator
{
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private readonly AzureOpenAIOptions _openAiOptions = openAiOptions.Value;

    private const string _provider = AiProviders.AzureOpenAI;

    private const string _operation = "QuestionGeneration";

    public async Task<GeneratedQuestion> GenerateQuestionAsync(
        GenerateQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.PreviousTurns);

        var targetRole = request.TargetRole;
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRole);
        var focusArea = request.FocusArea;
        ArgumentException.ThrowIfNullOrWhiteSpace(focusArea);

        if (request.TurnNumber <= 0)
        {
            throw new ArgumentException("Turn number must be greater than zero.", nameof(request));
        }

        if (request.QuestionCount <= 0)
        {
            throw new ArgumentException("Question count must be greater than zero.", nameof(request));
        }

        if (request.TurnNumber > request.QuestionCount)
        {
            throw new ArgumentException("Turn number cannot exceed question count.", nameof(request));
        }

        var prompt = PromptBuilder.BuildQuestionPrompt(
            targetRole,
            request.Seniority,
            request.InterviewType,
            focusArea,
            request.TurnNumber,
            request.QuestionCount,
            request.PreviousTurns,
            _aiOptions);

        var deploymentName = AzureOpenAIProvider.ResolveDeploymentName(_openAiOptions);
        var chatClient = aiClient.GetChatClient(deploymentName);
        var completionOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };
        var operationContext = new AiOperationContext(
            OperationName: _operation,
            PromptVersion: PromptVersions.QuestionGeneration,
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
                            PromptVersion: PromptVersions.QuestionGeneration,
                            Provider: _provider,
                            Model: deploymentName,
                            PromptTokens: usage?.InputTokenCount,
                            CompletionTokens: usage?.OutputTokenCount));
                }
                catch (RequestFailedException ex) when (AzureOpenAIProvider.IsTransient(ex.Status))
                {
                    throw new AiProviderTransientException(operationContext, $"Transient failure from {_provider}.", ex);
                }
                catch (RequestFailedException ex)
                {
                    throw new AiProviderUnavailableException(operationContext, $"{_provider} request failed.", ex);
                }
            },
            cancellationToken: cancellationToken);

        return new GeneratedQuestion(
            Text: response.Value.Text ?? throw new AiInvalidResponseException(operationContext, "AI response text was null."),
            Topic: response.Value.Topic ?? throw new AiInvalidResponseException(operationContext, "AI response topic was null."),
            AiMetadata: response.Metadata);
    }
}
