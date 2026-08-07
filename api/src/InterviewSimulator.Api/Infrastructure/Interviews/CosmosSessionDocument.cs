using System.Globalization;
using System.Text.Json.Serialization;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Infrastructure.Data;

namespace InterviewSimulator.Api.Infrastructure.Interviews;

public sealed class CosmosSessionDocument : IUserCosmosDocument
{
    public string Id { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string SessionId { get; init; } = string.Empty;

    public string Type { get; init; } = "session";

    public int SchemaVersion { get; init; } = 1;

    public string TargetRole { get; init; } = string.Empty;

    public string Seniority { get; init; } = string.Empty;

    public string FocusArea { get; init; } = string.Empty;

    public string InterviewType { get; init; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int QuestionCount { get; set; }

    public int AnsweredCount { get; set; }

    public CosmosSessionFeedbackDocument? Feedback { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    [JsonPropertyName("_etag")]
    public string? ETag { get; set; }

    public static CosmosSessionDocument FromDomain(InterviewSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new CosmosSessionDocument
        {
            Id = ToCosmosId(session.Id),
            SessionId = FormatSessionId(session.Id),
            UserId = session.UserId,
            TargetRole = session.TargetRole,
            Seniority = session.Seniority.ToString(),
            FocusArea = session.FocusArea,
            InterviewType = session.InterviewType.ToString(),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            QuestionCount = session.QuestionCount,
            AnsweredCount = session.AnsweredCount,
            Status = session.Status.ToString(),
            ETag = session.ConcurrencyToken,
            Feedback = session.Feedback is not null
                ? new CosmosSessionFeedbackDocument
                {
                    Score = session.Feedback.Score,
                    Summary = session.Feedback.Summary
                }
                : null
        };
    }

    public InterviewSession ToDomain() => InterviewSession.Restore(new InterviewSessionState(
        Id: Guid.Parse(SessionId),
        UserId: UserId,
        TargetRole: TargetRole,
        Seniority: Enum.Parse<SeniorityLevel>(Seniority),
        FocusArea: FocusArea,
        InterviewType: Enum.Parse<InterviewType>(InterviewType),
        Status: Enum.Parse<InterviewStatus>(Status),
        QuestionCount: QuestionCount,
        AnsweredCount: AnsweredCount,
        Feedback: Feedback is not null
            ? new InterviewFeedback(
                Score: Feedback.Score,
                Summary: Feedback.Summary)
            : null,
        CreatedAt: CreatedAt,
        UpdatedAt: UpdatedAt,
        StartedAt: StartedAt,
        CompletedAt: CompletedAt,
        ConcurrencyToken: ETag));

    public static string ToCosmosId(Guid sessionId) => $"session|{FormatSessionId(sessionId)}";

    private static string FormatSessionId(Guid sessionId) => sessionId == Guid.Empty
        ? throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId))
        : sessionId.ToString("D", CultureInfo.InvariantCulture);
}

public sealed class CosmosSessionFeedbackDocument
{
    public int Score { get; set; }

    public string? Summary { get; set; }
}
