using System.Globalization;

using InterviewSimulator.Api.Infrastructure.Data;

namespace InterviewSimulator.Api.Infrastructure.Interviews;

public sealed class CosmosSessionDocument : IUserCosmosDocument
{
    public string Id { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string SessionId { get; init; } = string.Empty;

    public string Type { get; init; } = "session";

    public int SchemaVersion { get; init; } = 1;

    public string Role { get; init; } = string.Empty;

    public string Seniority { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public string InterviewType { get; init; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int QuestionCount { get; set; }

    public int AnsweredCount { get; set; }

    public CosmosSessionSummaryDocument? Summary { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public static CosmosSessionDocument Create(
        Guid sessionId,
        string userId,
        string role,
        string seniority,
        string topic,
        string interviewType,
        DateTimeOffset createdAt,
        int questionCount,
        string status,
        int answeredCount)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
        }

        return new CosmosSessionDocument
        {
            Id = ToCosmosId(sessionId),
            SessionId = FormatSessionId(sessionId),
            UserId = userId,
            Role = role,
            Seniority = seniority,
            Topic = topic,
            InterviewType = interviewType,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            QuestionCount = questionCount,
            AnsweredCount = answeredCount,
            Status = status,
        };
    }

    public static string ToCosmosId(Guid sessionId) => $"session|{FormatSessionId(sessionId)}";

    private static string FormatSessionId(Guid sessionId) => sessionId == Guid.Empty
        ? throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId))
        : sessionId.ToString("D", CultureInfo.InvariantCulture);
}

public sealed class CosmosSessionSummaryDocument
{
    public decimal? OverallScore { get; set; }

    public string? Recommendation { get; set; }

    public string? Strengths { get; set; }

    public string? Weaknesses { get; set; }
}
