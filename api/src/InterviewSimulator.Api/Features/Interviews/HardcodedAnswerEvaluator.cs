using InterviewSimulator.Api.Features.Interviews.Ai;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed class HardcodedAnswerEvaluator : IAnswerEvaluator
{
    public Task<AnswerEvaluationResult> EvaluateAnswerAsync(
        EvaluateAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRequest(request);

        var rubric = EvaluationRubrics.GetForInterviewType(request.InterviewType);

        var dimensions = rubric.Dimensions
            .Select(dimension => new EvaluationDimension(
                key: dimension.Key,
                label: dimension.Label,
                score: new Score(80),
                feedback: new Feedback($"Stub feedback for {dimension.Label}.")))
            .ToArray();

        var overallScore = (int)Math.Round(dimensions.Average(d => d.Score.Value));

        var evaluation = new AnswerEvaluation(
            overallScore: new Score(overallScore),
            feedback: new Feedback("Stub evaluation feedback."),
            dimensions: dimensions);

        var metadata = new AiCallMetadata(
            PromptVersion: PromptVersions.HardcodedAnswerEvaluation,
            Provider: AiProviders.Hardcoded,
            Model: null,
            PromptTokens: null,
            CompletionTokens: null);

        return Task.FromResult(new AnswerEvaluationResult(evaluation, metadata));
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

        if (request.TurnNumber > request.QuestionCount)
        {
            throw new ArgumentException(
                "Turn number cannot exceed question count.",
                nameof(request));
        }
    }
}