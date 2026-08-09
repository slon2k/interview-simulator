using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Interviews.Ai;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed class InterviewTurn
{
    public Guid SessionId { get; private init; }

    public string UserId { get; private init; } = string.Empty;

    public int TurnNumber { get; private init; }

    public InterviewQuestion Question { get; private init; } = default!;

    public InterviewAnswer? Answer { get; private set; }

    public AnswerEvaluation? Evaluation { get; private set; }

    public AiCallMetadata? QuestionGenerationMetadata { get; private set; }

    public AiCallMetadata? AnswerEvaluationMetadata { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsAnswered => Answer is not null;

    public bool IsEvaluated => Evaluation is not null;

    public string? ConcurrencyToken { get; private set; }

    public void RecordAnswer(string answer, DateTimeOffset answeredAt)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException("Answer cannot be null or whitespace.", nameof(answer));
        }

        if (IsAnswered)
        {
            throw new DomainConflictException(Errors.TurnAlreadyAnswered);
        }

        if (answeredAt < CreatedAt)
        {
            throw new ArgumentException("Answered timestamp cannot be before created timestamp.", nameof(answeredAt));
        }

        var interviewAnswer = new InterviewAnswer(answer, answeredAt);
        Answer = interviewAnswer;
        UpdatedAt = answeredAt;
    }

    public void Evaluate(AnswerEvaluation evaluation, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(evaluation, nameof(evaluation));

        if (!IsAnswered)
        {
            throw new DomainConflictException(Errors.CannotEvaluateUnansweredTurn);
        }

        if (Answer is not null && updatedAt < Answer.AnsweredAt)
        {
            throw new ArgumentException("Updated timestamp cannot be before answered timestamp.", nameof(updatedAt));
        }

        if (updatedAt < CreatedAt)
        {
            throw new ArgumentException("Updated timestamp cannot be before created timestamp.", nameof(updatedAt));
        }

        if (IsEvaluated)
        {
            throw new DomainConflictException(Errors.TurnAlreadyEvaluated);
        }

        Evaluation = evaluation;
        AnswerEvaluationMetadata = evaluation.AiMetadata;
        UpdatedAt = updatedAt;
    }

    public void RecordQuestionGenerationMetadata(AiCallMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata, nameof(metadata));

        QuestionGenerationMetadata = metadata;
    }

    private InterviewTurn(
        Guid sessionId,
        string userId,
        int turnNumber,
        InterviewQuestion question,
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

        SessionId = sessionId;
        UserId = userId;
        TurnNumber = turnNumber;
        Question = question;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static InterviewTurn Create(
        Guid sessionId,
        string userId,
        int turnNumber,
        InterviewQuestion question,
        DateTimeOffset createdAt) => new(
            sessionId: sessionId,
            userId: userId,
            turnNumber: turnNumber,
            question: question,
            createdAt: createdAt);

    public InterviewTurnState ToState() => new(
        SessionId: SessionId,
        UserId: UserId,
        TurnNumber: TurnNumber,
        Question: Question,
        Answer: Answer,
        Evaluation: Evaluation,
        CreatedAt: CreatedAt,
        UpdatedAt: UpdatedAt,
        QuestionGenerationMetadata: QuestionGenerationMetadata,
        AnswerEvaluationMetadata: AnswerEvaluationMetadata,
        ConcurrencyToken: ConcurrencyToken);

    public static InterviewTurn Restore(InterviewTurnState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateInterviewTurnState(state);

        return new InterviewTurn(
            sessionId: state.SessionId,
            userId: state.UserId,
            turnNumber: state.TurnNumber,
            question: state.Question,
            createdAt: state.CreatedAt)
        {
            Answer = state.Answer,
            Evaluation = state.Evaluation,
            QuestionGenerationMetadata = state.QuestionGenerationMetadata,
            AnswerEvaluationMetadata = state.AnswerEvaluationMetadata,
            UpdatedAt = state.UpdatedAt,
            ConcurrencyToken = state.ConcurrencyToken
        };
    }

    private static void ValidateInterviewTurnState(InterviewTurnState state)
    {
        if (state.SessionId == Guid.Empty)
        {
            throw new ArgumentException("Session ID cannot be empty.", nameof(state));
        }

        if (string.IsNullOrWhiteSpace(state.UserId))
        {
            throw new ArgumentException("User ID cannot be null or whitespace.", nameof(state));
        }

        if (state.CreatedAt > state.UpdatedAt)
        {
            throw new ArgumentException("Updated timestamp cannot be before created timestamp.", nameof(state));
        }

        if (state.Answer is null && state.Evaluation is not null)
        {
            throw new ArgumentException("Cannot have evaluation without answer.", nameof(state));
        }

        if (state.Answer is not null && state.Answer.AnsweredAt < state.CreatedAt)
        {
            throw new ArgumentException("Answer timestamp cannot be before created timestamp.", nameof(state));
        }
    }

    public static class Errors
    {
        public static DomainError TurnAlreadyAnswered => new("Interviews.InterviewTurn.TurnAlreadyAnswered", "Cannot record answer for a turn that has already been answered.");
        public static DomainError CannotEvaluateUnansweredTurn => new("Interviews.InterviewTurn.CannotEvaluateUnansweredTurn", "Cannot evaluate an unanswered turn.");
        public static DomainError TurnAlreadyEvaluated => new("Interviews.InterviewTurn.TurnAlreadyEvaluated", "Cannot evaluate a turn that has already been evaluated.");
    }
}

public sealed record InterviewQuestion
{
    public string Text { get; }

    public string Topic { get; }

    public InterviewQuestion(string text, string topic)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Question text cannot be null or whitespace.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Question topic cannot be null or whitespace.", nameof(topic));
        }

        Text = text;
        Topic = topic;
    }
}

public sealed record InterviewAnswer
{
    public string Text { get; }

    public DateTimeOffset AnsweredAt { get; }

    public InterviewAnswer(string text, DateTimeOffset answeredAt)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Answer text cannot be null or whitespace.", nameof(text));
        }

        Text = text;
        AnsweredAt = answeredAt;
    }
}
public sealed record AnswerEvaluation(
    Score OverallScore,
    string Feedback,
    IReadOnlyList<EvaluationDimension> Dimensions,
    AiCallMetadata? AiMetadata = null);


public sealed record EvaluationDimension(
    string Key,
    string Label,
    Score Score,
    Feedback Feedback);

public sealed record InterviewTurnState(
    Guid SessionId,
    string UserId,
    int TurnNumber,
    InterviewQuestion Question,
    InterviewAnswer? Answer,
    AnswerEvaluation? Evaluation,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    AiCallMetadata? QuestionGenerationMetadata = null,
    AiCallMetadata? AnswerEvaluationMetadata = null,
    string? ConcurrencyToken = null);

public readonly record struct Score
{
    public int Value { get; }

    public Score(int value)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Score must be between 0 and 100.");
        }

        Value = value;
    }

    public static implicit operator int(Score score) => score.Value;
}

public readonly record struct Feedback
{
    public string Text { get; }

    public Feedback(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Feedback text cannot be null or whitespace.", nameof(text));
        }

        Text = text;
    }

    public static implicit operator string(Feedback feedback) => feedback.Text;
}
