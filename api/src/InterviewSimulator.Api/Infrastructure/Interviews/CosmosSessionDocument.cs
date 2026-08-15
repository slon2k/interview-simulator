using System.Globalization;
using System.Text.Json.Serialization;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;
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

    public CosmosSessionResultDocument? SessionResult { get; set; }

    public CosmosInterviewSummaryDocument? Summary { get; set; }

    [JsonPropertyName("summaryAi")]
    public CosmosAiMetadataDocument? SummaryMetadata { get; set; }

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
            SessionResult = session.SessionResult is not null
                ? new CosmosSessionResultDocument
                {
                    TotalScore = session.SessionResult.OverallScore,
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
        SessionResult: SessionResult is not null
            ? new SessionResult(new Score(SessionResult.TotalScore))
            : null,
        InterviewSummary: Summary is not null
            ? new InterviewSummary(Summary.Text, Summary.CreatedAt)
            : null,
        SummaryMetadata: ToDomainMetadata(SummaryMetadata),
        CreatedAt: CreatedAt,
        UpdatedAt: UpdatedAt,
        StartedAt: StartedAt,
        CompletedAt: CompletedAt,
        ConcurrencyToken: ETag));

    private static AiCallMetadata? ToDomainMetadata(CosmosAiMetadataDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(document.PromptVersion) ||
            string.IsNullOrWhiteSpace(document.Provider))
        {
            return null;
        }

        return new AiCallMetadata(
            PromptVersion: document.PromptVersion,
            Provider: document.Provider,
            Model: document.Model,
            PromptTokens: document.PromptTokens,
            CompletionTokens: document.CompletionTokens);
    }

    public static string ToCosmosId(Guid sessionId) => $"session|{FormatSessionId(sessionId)}";

    private static string FormatSessionId(Guid sessionId) => sessionId == Guid.Empty
        ? throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId))
        : sessionId.ToString("D", CultureInfo.InvariantCulture);
}

public sealed class CosmosSessionResultDocument
{
    public int TotalScore { get; set; }
}

public sealed class CosmosInterviewSummaryDocument
{
    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
