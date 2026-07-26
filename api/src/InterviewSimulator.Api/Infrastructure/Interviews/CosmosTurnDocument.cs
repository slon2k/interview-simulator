using System.Globalization;

using InterviewSimulator.Api.Infrastructure.Data;

namespace InterviewSimulator.Api.Infrastructure.Interviews;

public sealed class CosmosTurnDocument : ICosmosDocument, IUserCosmosDocument
{
    public string Id { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string SessionId { get; init; } = string.Empty;

    public int TurnNumber { get; init; }

    public string Type { get; init; } = "turn";

    public int SchemaVersion { get; init; } = 1;

    public CosmosQuestionDocument Question { get; init; } = new();

    public CosmosAnswerDocument? Answer { get; set; }

    public CosmosEvaluationDocument? Evaluation { get; set; }

    public CosmosAiMetadataDocument? AiMetadata { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? AnsweredAt { get; set; }

    public DateTimeOffset? EvaluatedAt { get; set; }

    public static CosmosTurnDocument Create(
        Guid sessionId,
        string userId,
        int turnNumber,
        CosmosQuestionDocument question,
        DateTimeOffset createdAt)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
        }

        if (turnNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnNumber), "Turn number must be greater than zero.");
        }

        return new CosmosTurnDocument
        {
            Id = ToCosmosId(sessionId, turnNumber),
            SessionId = sessionId.ToString("N", CultureInfo.InvariantCulture),
            UserId = userId,
            TurnNumber = turnNumber,
            Question = question,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    public static string ToCosmosId(Guid sessionId, int turnNumber)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));
        }

        if (turnNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnNumber), "Turn number must be greater than zero.");
        }

        return $"turn|{sessionId}|{turnNumber:D3}";
    }
}

public sealed class CosmosQuestionDocument
{
    public string Text { get; init; } = string.Empty;

    public string? Category { get; init; }

    public string? Difficulty { get; init; }
}

public sealed class CosmosAnswerDocument
{
    public string Text { get; init; } = string.Empty;
}

public sealed class CosmosEvaluationDocument
{
    public decimal? Score { get; init; }

    public string? Feedback { get; init; }

    public string? SuggestedAnswer { get; init; }
}

public sealed class CosmosAiMetadataDocument
{
    public string? Provider { get; init; }

    public string? Model { get; init; }

    public string? PromptVersion { get; init; }

    public int? PromptTokens { get; init; }

    public int? CompletionTokens { get; init; }
}