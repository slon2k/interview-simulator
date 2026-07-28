using System.Globalization;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Infrastructure.Data;

namespace InterviewSimulator.Api.Infrastructure.Interviews;

public sealed class CosmosTurnDocument : IUserCosmosDocument
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

        ArgumentNullException.ThrowIfNull(question, nameof(question));

        return new CosmosTurnDocument
        {
            Id = ToCosmosId(sessionId, turnNumber),
            SessionId = FormatSessionId(sessionId),
            UserId = userId,
            TurnNumber = turnNumber,
            Question = question,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    public static CosmosTurnDocument FromDomain(InterviewTurn turn)
    {
        ArgumentNullException.ThrowIfNull(turn, nameof(turn));

        return new CosmosTurnDocument
        {
            Id = ToCosmosId(turn.SessionId, turn.TurnNumber),
            SessionId = FormatSessionId(turn.SessionId),
            UserId = turn.UserId,
            TurnNumber = turn.TurnNumber,
            Question = new CosmosQuestionDocument
            {
                Text = turn.Question.Text,
                Topic = turn.Question.Topic,
            },
            Answer = turn.Answer is not null
                ? new CosmosAnswerDocument { Text = turn.Answer.Text }
                : null,
            Evaluation = turn.Evaluation is not null
                ? new CosmosEvaluationDocument
                {
                    Score = turn.Evaluation.Score,
                    Feedback = turn.Evaluation.Feedback,
                }
                : null,
            CreatedAt = turn.CreatedAt,
            UpdatedAt = turn.UpdatedAt,
            AnsweredAt = turn.Answer?.AnsweredAt,
        };
    }

    public InterviewTurn ToDomain()
    {
        return InterviewTurn.Restore(
            state: new InterviewTurnState(
                SessionId: Guid.Parse(SessionId, CultureInfo.InvariantCulture),
                UserId: UserId,
                TurnNumber: TurnNumber,
                Question: new InterviewQuestion(Question.Text, Question.Topic),
                Answer: Answer is not null
                    ? new InterviewAnswer(Answer.Text, AnsweredAt ?? CreatedAt)
                    : null,
                Evaluation: Evaluation is not null
                    ? new AnswerEvaluation(Evaluation.Score, Evaluation.Feedback)
                    : null,
                CreatedAt: CreatedAt,
                UpdatedAt: UpdatedAt));
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

        return $"turn|{FormatSessionId(sessionId)}|{turnNumber:D3}";
    }

    private static string FormatSessionId(Guid sessionId) => sessionId == Guid.Empty
        ? throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId))
        : sessionId.ToString("D", CultureInfo.InvariantCulture);
}

public sealed class CosmosQuestionDocument
{
    public string Text { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;
}

public sealed class CosmosAnswerDocument
{
    public string Text { get; init; } = string.Empty;
}

public sealed class CosmosEvaluationDocument
{
    public int Score { get; init; }

    public string Feedback { get; init; } = string.Empty;
}

public sealed class CosmosAiMetadataDocument
{
    public string? Provider { get; init; }

    public string? Model { get; init; }

    public string? PromptVersion { get; init; }

    public int? PromptTokens { get; init; }

    public int? CompletionTokens { get; init; }
}