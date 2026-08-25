using InterviewSimulator.Api.Features.Interviews;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Dashboard;

public class DashboardMetricsCalculator(IOptions<DashboardOptions> dashboardOptions)
{
    private readonly DashboardOptions _dashboardOptions = dashboardOptions.Value;

    public DashboardResponse Calculate(
        IReadOnlyCollection<DashboardSessionProjection> sessions,
        IReadOnlyCollection<DashboardDimensionProjection> dimensions)
    {
        var completedSessions = sessions
            .Where(x => x.Status == InterviewStatus.Completed)
            .ToArray();

        var scoredCompletedSessions = completedSessions
            .Where(x => x.AggregateScore.HasValue)
            .ToArray();

        var recentSessions = completedSessions
            .Where(s => s.CompletedAt.HasValue)
            .OrderByDescending(s => s.CompletedAt!.Value)
            .Take(_dashboardOptions.RecentSessionsLimit)
            .Select(s => new DashboardRecentSessionResponse(
                s.SessionId,
                s.TargetRole,
                s.FocusArea,
                s.InterviewType.ToContract(),
                s.CompletedAt!.Value,
                s.AggregateScore))
            .ToArray();

        var scoreTrend = scoredCompletedSessions
            .Where(s => s.CompletedAt.HasValue)
            .OrderByDescending(s => s.CompletedAt!.Value)
            .Take(_dashboardOptions.ScoreTrendLimit)
            .OrderBy(s => s.CompletedAt!.Value)
            .Select(s => new DashboardScoreTrendPointResponse(
                s.SessionId,
                s.CompletedAt!.Value,
                s.AggregateScore!.Value))
            .ToArray();

        var scoresByFocusArea = scoredCompletedSessions
            .GroupBy(s => s.FocusArea)
            .OrderBy(g => g.Key)
            .Select(g => new DashboardScoreByFocusAreaResponse(
                g.Key,
                RoundAverage(g.Select(s => s.AggregateScore!.Value)),
                g.Count()))
            .ToArray();

        var scoresByInterviewType = scoredCompletedSessions
            .GroupBy(s => s.InterviewType)
            .OrderBy(g => g.Key)
            .Select(g => new DashboardScoreByInterviewTypeResponse(
                g.Key.ToContract(),
                RoundAverage(g.Select(s => s.AggregateScore!.Value)),
                g.Count()))
            .ToArray();

        var scoredCompletedSessionIds = scoredCompletedSessions
            .Select(x => x.SessionId)
            .ToHashSet();

        var scoresByDimension = dimensions
            .Where(d => scoredCompletedSessionIds.Contains(d.SessionId))
            .GroupBy(d => d.Key)
            .OrderBy(g => g.Key)
            .Select(g => new DashboardScoreByDimensionResponse(
                g.Key,
                g.First().Label,
                RoundAverage(g.Select(d => d.Score)),
                g.Count()))
            .ToArray();

        return new DashboardResponse(
            TotalCompletedSessions: completedSessions.Length,
            AverageScore: scoredCompletedSessions.Length != 0
                ? RoundAverage(scoredCompletedSessions.Select(s => s.AggregateScore!.Value))
                : null,
            ScoreTrend: scoreTrend,
            ScoresByFocusArea: scoresByFocusArea,
            ScoresByInterviewType: scoresByInterviewType,
            ScoresByDimension: scoresByDimension,
            RecentSessions: recentSessions);
    }

    private static int RoundAverage(IEnumerable<int> values)
    {
        return (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);
    }
}

public sealed record DashboardSessionProjection(
    Guid SessionId,
    string TargetRole,
    string FocusArea,
    InterviewType InterviewType,
    InterviewStatus Status,
    DateTimeOffset? CompletedAt,
    int? AggregateScore);

public sealed record DashboardDimensionProjection(
    Guid SessionId,
    string Key,
    string Label,
    int Score);
