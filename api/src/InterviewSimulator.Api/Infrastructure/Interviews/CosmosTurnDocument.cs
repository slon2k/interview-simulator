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

    public CosmosAiMetadataDocument? QuestionGenerationMetadata { get; set; }

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
            QuestionGenerationMetadata = turn.QuestionGenerationMetadata is not null
                ? new CosmosAiMetadataDocument
                {
                    Provider = turn.QuestionGenerationMetadata.Provider,
                    Model = turn.QuestionGenerationMetadata.Model,
                    PromptVersion = turn.QuestionGenerationMetadata.PromptVersion,
                    PromptTokens = turn.QuestionGenerationMetadata.PromptTokens,
                    CompletionTokens = turn.QuestionGenerationMetadata.CompletionTokens
                }
                : null,
            AnswerEvaluationMetadata = turn.AnswerEvaluationMetadata is not null
                ? new CosmosAiMetadataDocument
                {
                    Provider = turn.AnswerEvaluationMetadata.Provider,
                    Model = turn.AnswerEvaluationMetadata.Model,
                    PromptVersion = turn.AnswerEvaluationMetadata.PromptVersion,
                    PromptTokens = turn.AnswerEvaluationMetadata.PromptTokens,
                    CompletionTokens = turn.AnswerEvaluationMetadata.CompletionTokens
                }
                : null,
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
                QuestionGenerationMetadata: QuestionGenerationMetadata is not null
                    ? new AiCallMetadata(
                        PromptVersion: QuestionGenerationMetadata.PromptVersion ?? string.Empty,
                        Provider: QuestionGenerationMetadata.Provider ?? string.Empty,
                        Model: QuestionGenerationMetadata.Model,
                        PromptTokens: QuestionGenerationMetadata.PromptTokens,
                        CompletionTokens: QuestionGenerationMetadata.CompletionTokens)
                    : null,
                AnswerEvaluationMetadata: AnswerEvaluationMetadata is not null
                    ? new AiCallMetadata(
                        PromptVersion: AnswerEvaluationMetadata.PromptVersion ?? string.Empty,
                        Provider: AnswerEvaluationMetadata.Provider ?? string.Empty,
                        Model: AnswerEvaluationMetadata.Model,
                        PromptTokens: AnswerEvaluationMetadata.PromptTokens,
                        CompletionTokens: AnswerEvaluationMetadata.CompletionTokens)
                    : null,
                Answer: Answer is not null
                    ? new InterviewAnswer(Answer.Text, AnsweredAt ?? CreatedAt)
                    : null,
                Evaluation: Evaluation is not null
                    ? new AnswerEvaluation(
                        new Score(Evaluation.OverallScore),
                        Evaluation.Feedback,
                        [.. Evaluation.Dimensions.Select(d => new EvaluationDimension(
                            Key: d.Key,
                            Label: d.Label,
                            Score: new Score(d.Score),
                            Feedback: new Feedback(d.Feedback)
                        ))])
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