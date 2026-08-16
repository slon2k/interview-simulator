namespace InterviewSimulator.Api.Features.Interviews;

public sealed class SessionSummaryService(IInterviewStore store, ISessionSummarizer summarizer, TimeProvider timeProvider)
{
    public async Task<InterviewSession> CreateSummaryAsync(
        Guid sessionId,
        string userId,
        CancellationToken cancellationToken)
    {
        var session = await store.GetSessionAsync(userId, sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found for user {userId}");

        var turns = await store.ListTurnsAsync(userId, sessionId, cancellationToken);

        var summaryRequest = new SessionSummaryRequest(
            TargetRole: session.TargetRole,
            FocusArea: session.FocusArea,
            Seniority: session.Seniority,
            InterviewType: session.InterviewType,
            Turns: [.. turns.Select(MapTurn)]);

        var summary = await summarizer.GenerateSummaryAsync(summaryRequest, cancellationToken) ?? throw new InvalidOperationException($"Failed to generate summary for session {sessionId} for user {userId}");
        var updatedAt = timeProvider.GetUtcNow();
        session.RecordSummary(new InterviewSummary(summary.Summary, updatedAt), summary.AiMetadata, updatedAt);
        await store.UpdateSessionAsync(session, cancellationToken);

        return session;
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
