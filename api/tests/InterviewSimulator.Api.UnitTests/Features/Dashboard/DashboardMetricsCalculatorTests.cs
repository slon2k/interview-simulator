using InterviewSimulator.Api.Features.Dashboard;
using InterviewSimulator.Api.Features.Interviews;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.UnitTests.Features.Dashboard;

public sealed class DashboardMetricsCalculatorTests
{
    [Fact]
    public void Calculate_ExcludesNonCompletedAndUnscoredSessionsFromScoreMetrics()
    {
        var completedScored = CreateSession(
            targetRole: "Engineer",
            focusArea: "Algorithms",
            interviewType: InterviewType.Technical,
            status: InterviewStatus.Completed,
            completedAt: DateTimeOffset.Parse("2026-08-01T10:00:00Z"),
            aggregateScore: 80);
        var completedUnscored = CreateSession(
            focusArea: "Communication",
            status: InterviewStatus.Completed,
            completedAt: DateTimeOffset.Parse("2026-08-02T10:00:00Z"));
        var activeScored = CreateSession(
            focusArea: "Algorithms",
            status: InterviewStatus.Active,
            completedAt: null,
            aggregateScore: 100);

        var result = CreateCalculator().Calculate(
            [completedScored, completedUnscored, activeScored],
            [
                new(completedScored.SessionId, "clarity", "Clarity", 75),
                new(completedUnscored.SessionId, "clarity", "Clarity", 100),
                new(activeScored.SessionId, "clarity", "Clarity", 100),
            ]);

        Assert.Equal(2, result.TotalCompletedSessions);
        Assert.Equal(80, result.AverageScore);
        Assert.Single(result.ScoresByFocusArea);
        Assert.Single(result.ScoresByInterviewType);
        Assert.Single(result.ScoresByDimension);
        Assert.Equal(75, result.ScoresByDimension[0].Score);
        Assert.Equal(2, result.RecentSessions.Count);
        Assert.Contains(result.RecentSessions, session => session.TotalScore is null);
    }

    [Fact]
    public void Calculate_AppliesRecentAndTrendLimitsWithExpectedOrdering()
    {
        var sessions = Enumerable.Range(1, 4)
            .Select(index => CreateSession(
                targetRole: $"Role {index}",
                completedAt: DateTimeOffset.Parse($"2026-08-0{index}T10:00:00Z"),
                aggregateScore: index * 10))
            .ToArray();

        var result = CreateCalculator(recentSessionsLimit: 2, scoreTrendLimit: 3)
            .Calculate(sessions, []);

        Assert.Equal(
            new[] { sessions[3].SessionId, sessions[2].SessionId },
            result.RecentSessions.Select(session => session.Id));
        Assert.Equal(
            new[] { sessions[1].SessionId, sessions[2].SessionId, sessions[3].SessionId },
            result.ScoreTrend.Select(point => point.InterviewId));
    }

    [Fact]
    public void Calculate_GroupsAndRoundsScoresAwayFromZero()
    {
        var technicalFirst = CreateSession(
            focusArea: "Algorithms",
            interviewType: InterviewType.Technical,
            aggregateScore: 80);
        var technicalSecond = CreateSession(
            focusArea: "Algorithms",
            interviewType: InterviewType.Technical,
            aggregateScore: 81);
        var behavioral = CreateSession(
            focusArea: "Communication",
            interviewType: InterviewType.Behavioral,
            aggregateScore: 79);

        var result = CreateCalculator().Calculate(
            [technicalFirst, technicalSecond, behavioral],
            []);

        Assert.Equal(80, result.AverageScore);
        Assert.Equal(
            [new DashboardScoreByFocusAreaResponse("Algorithms", 81, 2), new("Communication", 79, 1)],
            result.ScoresByFocusArea);
        Assert.Equal(
            [
                new DashboardScoreByInterviewTypeResponse(InterviewTypeContract.Technical, 81, 2),
                new(InterviewTypeContract.Behavioral, 79, 1),
            ],
            result.ScoresByInterviewType);
    }

    [Fact]
    public void Calculate_GroupsDimensionsOnlyForScoredCompletedSessions()
    {
        var included = CreateSession(aggregateScore: 90);
        var excluded = CreateSession(status: InterviewStatus.Active, aggregateScore: 100);

        var result = CreateCalculator().Calculate(
            [included, excluded],
            [
                new(included.SessionId, "clarity", "Clarity", 80),
                new(included.SessionId, "clarity", "Clarity", 81),
                new(included.SessionId, "depth", "Depth", 70),
                new(excluded.SessionId, "clarity", "Clarity", 100),
            ]);

        Assert.Equal(
            [
                new DashboardScoreByDimensionResponse("clarity", "Clarity", 81, 2),
                new("depth", "Depth", 70, 1),
            ],
            result.ScoresByDimension);
    }

    private static DashboardMetricsCalculator CreateCalculator(
        int recentSessionsLimit = 5,
        int scoreTrendLimit = 5)
    {
        return new DashboardMetricsCalculator(Microsoft.Extensions.Options.Options.Create(new DashboardOptions
        {
            RecentSessionsLimit = recentSessionsLimit,
            ScoreTrendLimit = scoreTrendLimit,
        }));
    }

    private static DashboardSessionProjection CreateSession(
        string targetRole = "Engineer",
        string focusArea = "Algorithms",
        InterviewType interviewType = InterviewType.Technical,
        InterviewStatus status = InterviewStatus.Completed,
        DateTimeOffset? completedAt = null,
        int? aggregateScore = null)
    {
        return new(
            Guid.NewGuid(),
            targetRole,
            focusArea,
            interviewType,
            status,
            completedAt ?? DateTimeOffset.UtcNow,
            aggregateScore);
    }
}