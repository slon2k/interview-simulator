namespace InterviewSimulator.Api.Features.Interviews;

public interface ISessionSummarizer
{
    Task<SessionSummaryResult> GenerateSummaryAsync(
        SessionSummaryRequest request,
        CancellationToken cancellationToken = default);
}
