using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.Features.Dashboard;

public sealed record DashboardResponse(
    int TotalCompletedSessions,
    int? AverageScore,
    IReadOnlyList<DashboardScoreTrendPointResponse> ScoreTrend,
    IReadOnlyList<DashboardScoreByFocusAreaResponse> ScoresByFocusArea,
    IReadOnlyList<DashboardScoreByInterviewTypeResponse> ScoresByInterviewType,
    IReadOnlyList<DashboardScoreByDimensionResponse> ScoresByDimension,
    IReadOnlyList<DashboardRecentSessionResponse> RecentSessions);

public sealed record DashboardScoreTrendPointResponse(
    Guid InterviewId,
    DateTimeOffset CompletedAt,
    int TotalScore);

public sealed record DashboardScoreByFocusAreaResponse(
    string FocusArea,
    int Score,
    int SessionCount);

public sealed record DashboardScoreByInterviewTypeResponse(
    InterviewTypeContract InterviewType,
    int Score,
    int SessionCount);

public sealed record DashboardScoreByDimensionResponse(
    string Key,
    string Label,
    int Score,
    int SampleCount);

public sealed record DashboardRecentSessionResponse(
    Guid Id,
    string TargetRole,
    string FocusArea,
    InterviewTypeContract InterviewType,
    DateTimeOffset CompletedAt,
    int? TotalScore);