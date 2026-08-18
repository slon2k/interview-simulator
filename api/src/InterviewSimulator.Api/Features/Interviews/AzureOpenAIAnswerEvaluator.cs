using Azure;
using Azure.AI.OpenAI;

using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.Options;

using Microsoft.Extensions.Options;

using OpenAI.Chat;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed class AzureOpenAIAnswerEvaluator(
    AiStructuredOutputRunner<AnswerEvaluationResponse> runner,
    AzureOpenAIClient aiClient,
    IOptions<AiOptions> aiOptions,
    IOptions<AzureOpenAIOptions> openAiOptions) : IAnswerEvaluator
{
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private readonly AzureOpenAIOptions _openAiOptions = openAiOptions.Value;

    private const string _provider = AiProviders.AzureOpenAI;

    private const string _operation = "AnswerEvaluation";

    public async Task<AnswerEvaluationResult> EvaluateAnswerAsync(
        EvaluateAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRequest(request);

        var rubric = EvaluationRubrics.GetForInterviewType(request.InterviewType);
        var deploymentName = AzureOpenAIProvider.ResolveDeploymentName(_openAiOptions);

        var prompt = PromptBuilder.BuildAnswerEvaluationPrompt(
            request.TargetRole,
            request.Seniority,
            request.InterviewType,
            request.FocusArea,
            request.TurnNumber,
            request.QuestionCount,
            request.QuestionText,
            request.QuestionTopic,
            request.AnswerText,
            request.PreviousTurns,
            rubric,
            _aiOptions);

        var chatClient = aiClient.GetChatClient(deploymentName);
        var completionOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };
        var operationContext = new AiOperationContext(
            OperationName: _operation,
            PromptVersion: rubric.PromptVersion,
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
                            PromptVersion: rubric.PromptVersion,
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

        var rubricIndex = rubric.Dimensions.ToDictionary(d => d.Key, d => d.Label, StringComparer.OrdinalIgnoreCase);
        var responseDimensions = response.Value.Dimensions!;

        // Verify all rubric keys are present before constructing the domain object.
        var missingKeys = rubric.Dimensions
            .Select(d => d.Key)
            .Except(responseDimensions.Select(d => d.Key ?? string.Empty), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingKeys.Length > 0)
        {
            throw new AiInvalidResponseException(
                operationContext,
                "AI evaluation response is missing required rubric dimension keys.",
                [.. missingKeys.Select(k => $"Missing dimension key: {k}")]);
        }

        var dimensions = responseDimensions
            .Where(d => rubricIndex.ContainsKey(d.Key!))
            .Select(d => new EvaluationDimension(
                key: d.Key!,
                label: rubricIndex[d.Key!],
                score: new Score(d.Score!.Value),
                feedback: new Feedback(d.Feedback!)))
            .ToArray();

        var overallScore = (int)Math.Round(dimensions.Average(d => d.Score.Value));

        var evaluation = new AnswerEvaluation(
            overallScore: new Score(overallScore),
            feedback: new Feedback(response.Value.Feedback!),
            dimensions: dimensions);

        return new AnswerEvaluationResult(evaluation, response.Metadata);
    }

    private static void ValidateRequest(EvaluateAnswerRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetRole);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FocusArea);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.QuestionText);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.QuestionTopic);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AnswerText);
        ArgumentNullException.ThrowIfNull(request.PreviousTurns);

        if (request.TurnNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Turn number must be greater than zero.");
        }

        if (request.QuestionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Question count must be greater than zero.");
        }
    }
}