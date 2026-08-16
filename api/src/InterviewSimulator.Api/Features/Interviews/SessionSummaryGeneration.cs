namespace InterviewSimulator.Api.Features.Interviews;

internal static class SessionSummaryGeneration
{
    public static async Task GenerateBestEffortAsync(
        InterviewSession session,
        IReadOnlyList<InterviewTurn> turns,
        ISessionSummarizer summarizer,
        IInterviewStore store,
        TimeProvider timeProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new SessionSummaryRequest(
                Turns: [.. turns
                    .Where(turn => turn.Answer is not null && turn.Evaluation is not null)
                    .OrderBy(turn => turn.TurnNumber)
                    .Select(MapTurn)],
                TargetRole: session.TargetRole,
                Seniority: session.Seniority,
                InterviewType: session.InterviewType,
                FocusArea: session.FocusArea);

            var result = await summarizer.GenerateSummaryAsync(request, cancellationToken);
            var generatedAt = timeProvider.GetUtcNow();

            session.RecordSummary(
                interviewSummary: new InterviewSummary(
                    Text: result.Summary,
                    CreatedAt: generatedAt),
                aiCallMetadata: result.AiMetadata,
                updatedAt: generatedAt);

            await store.UpdateSessionAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Session summary generation failed for interview {InterviewId}. The completed session remains available without a summary.",
                session.Id);
        }
    }

    private static SessionSummaryTurn MapTurn(InterviewTurn turn)
    {
        var evaluation = turn.Evaluation!;

        return new SessionSummaryTurn(
            TurnNumber: turn.TurnNumber,
            QuestionText: turn.Question.Text,
            QuestionTopic: turn.Question.Topic,
            AnswerText: turn.Answer!.Text,
            OverallScore: evaluation.OverallScore,
            Feedback: evaluation.Feedback,
            Dimensions: [.. evaluation.Dimensions.Select(d => new SessionSummaryDimension(
                Key: d.Key,
                Score: d.Score,
                Feedback: d.Feedback))]);
    }
}
