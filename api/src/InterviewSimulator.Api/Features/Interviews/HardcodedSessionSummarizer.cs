using InterviewSimulator.Api.Features.Interviews.Ai;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed class HardcodedSessionSummarizer : ISessionSummarizer
{
    public Task<SessionSummaryResult> GenerateSummaryAsync(
        SessionSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var summary = request.Turns.Count == 0
            ? "No evaluations were recorded for this interview."
            : $"The candidate completed {request.Turns.Count} turn(s) with an average score of {request.Turns.Average(t => t.OverallScore):0.0}.";

        var metadata = new AiCallMetadata(
            PromptVersion: PromptVersions.SessionSummary,
            Provider: AiProviders.Hardcoded,
            Model: null,
            PromptTokens: null,
            CompletionTokens: null);

        return Task.FromResult(new SessionSummaryResult(summary, metadata));
    }
}
