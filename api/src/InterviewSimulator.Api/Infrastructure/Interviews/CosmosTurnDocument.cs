using System.Globalization;
using System.Text.Json.Serialization;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;
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

    [JsonPropertyName("questionAi")]
    public CosmosAiMetadataDocument? QuestionGenerationMetadata { get; set; }

    [JsonPropertyName("evaluationAi")]
    public CosmosAiMetadataDocument? AnswerEvaluationMetadata { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? AnsweredAt { get; set; }

    [JsonPropertyName("_etag")]
    public string? ETag { get; set; }

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
                    OverallScore = turn.Evaluation.OverallScore,
                    Feedback = turn.Evaluation.Feedback,
                    Dimensions = [.. turn.Evaluation.Dimensions.Select(d => new CosmosEvaluationDimensionDocument
                    {
                        Key = d.Key,
                        Label = d.Label,
                        Score = d.Score,
                        Feedback = d.Feedback
                    })]
                }
                : null,
            QuestionGenerationMetadata = FromDomainMetadata(turn.QuestionGenerationMetadata),
            AnswerEvaluationMetadata = FromDomainMetadata(turn.AnswerEvaluationMetadata),
            CreatedAt = turn.CreatedAt,
            UpdatedAt = turn.UpdatedAt,
            AnsweredAt = turn.Answer?.AnsweredAt,
            ETag = turn.ConcurrencyToken,
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
                QuestionGenerationMetadata: ToDomainMetadata(QuestionGenerationMetadata),
                AnswerEvaluationMetadata: ToDomainMetadata(AnswerEvaluationMetadata),
                Answer: Answer is not null
                    ? new InterviewAnswer(Answer.Text, AnsweredAt ?? CreatedAt)
                    : null,
                Evaluation: Evaluation is not null
                    ? new AnswerEvaluation(
                        new Score(Evaluation.OverallScore),
                        new Feedback(Evaluation.Feedback),
                        [.. (Evaluation.Dimensions ?? []).Select(d => new EvaluationDimension(
                            d.Key,
                            d.Label,
                            new Score(d.Score),
                            new Feedback(d.Feedback)))])
                    : null,
                CreatedAt: CreatedAt,
                UpdatedAt: UpdatedAt,
                ConcurrencyToken: ETag));
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

    private static CosmosAiMetadataDocument? FromDomainMetadata(AiCallMetadata? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        return new CosmosAiMetadataDocument
        {
            Provider = metadata.Provider,
            Model = metadata.Model,
            PromptVersion = metadata.PromptVersion,
            PromptTokens = metadata.PromptTokens,
            CompletionTokens = metadata.CompletionTokens
        };
    }

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
    public int OverallScore { get; init; }

    public string Feedback { get; init; } = string.Empty;

    public IReadOnlyList<CosmosEvaluationDimensionDocument> Dimensions { get; init; } = [];
}

public sealed class CosmosEvaluationDimensionDocument
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

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