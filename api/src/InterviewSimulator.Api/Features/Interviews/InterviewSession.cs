using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Interviews.Ai;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed class InterviewSession
{
    public Guid Id { get; private init; }

    public string UserId { get; private init; } = string.Empty;

    public InterviewStatus Status { get; private set; }

    public string TargetRole { get; private init; } = string.Empty;

    public string FocusArea { get; private init; } = string.Empty;

    public InterviewType InterviewType { get; private init; }

    public SeniorityLevel Seniority { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public int QuestionCount { get; private init; }

    // Bounds the summary prompt, which covers every turn.
    public const int MaxQuestionCount = 20;

    public int AnsweredCount { get; private set; }

    public SessionResult? SessionResult { get; private set; }

    public InterviewSummary? InterviewSummary { get; private set; }

    public AiCallMetadata? SummaryMetadata { get; private set; }

    public string? ConcurrencyToken { get; private set; }

    private InterviewSession()
    {
    }

    public static InterviewSession Create(
        string userId,
        string targetRole,
        string focusArea,
        SeniorityLevel seniority,
        InterviewType interviewType,
        DateTimeOffset createdAt,
        int questionCount)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(targetRole))
        {
            throw new ArgumentException("Target role cannot be null or whitespace.", nameof(targetRole));
        }

        if (string.IsNullOrWhiteSpace(focusArea))
        {
            throw new ArgumentException("Focus area cannot be null or whitespace.", nameof(focusArea));
        }

        if (questionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(questionCount), "Question count must be greater than zero.");
        }

        if (questionCount > MaxQuestionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(questionCount), $"Question count cannot exceed {MaxQuestionCount}.");
        }

        var sessionId = Guid.NewGuid();

        return new InterviewSession
        {
            Id = sessionId,
            UserId = userId,
            TargetRole = targetRole,
            FocusArea = focusArea,
            Status = InterviewStatus.Created,
            Seniority = seniority,
            InterviewType = interviewType,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            QuestionCount = questionCount,
            AnsweredCount = 0,
            ConcurrencyToken = null,
        };
    }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status != InterviewStatus.Created)
        {
            throw new DomainConflictException(Errors.SessionNotCreated);
        }

        if (startedAt < CreatedAt)
        {
            throw new InvalidOperationException("Started timestamp cannot be before created timestamp.");
        }

        Status = InterviewStatus.Active;
        StartedAt = startedAt;
        UpdatedAt = startedAt;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status == InterviewStatus.Completed)
        {
            return; // Idempotent: Allow completing an already-completed session
        }

        if (Status != InterviewStatus.Active)
        {
            throw new DomainConflictException(Errors.SessionNotActive);
        }

        if (StartedAt is null)
        {
            throw new DomainConflictException(Errors.ActiveInterviewMissingStartedAt);
        }

        if (completedAt < StartedAt.Value)
        {
            throw new DomainConflictException(Errors.CompletedBeforeStartedAt);
        }

        MarkCompleted(completedAt);
    }

    public bool RecordAnswer(SessionResult result, DateTimeOffset answeredAt)
    {
        if (Status != InterviewStatus.Active)
        {
            throw new DomainConflictException(Errors.SessionNotActive);
        }

        if (StartedAt is null)
        {
            throw new DomainConflictException(Errors.ActiveInterviewMissingStartedAt);
        }

        if (AnsweredCount >= QuestionCount)
        {
            throw new DomainConflictException(Errors.AnsweredBeyondQuestionCount);
        }

        if (answeredAt < StartedAt.Value)
        {
            throw new DomainConflictException(Errors.AnsweredBeforeStartedAt);
        }

        AnsweredCount++;
        SessionResult = result;

        if (AnsweredCount == QuestionCount)
        {
            MarkCompleted(answeredAt);
            return true;
        }

        UpdatedAt = answeredAt;
        return false;
    }

    private void MarkCompleted(DateTimeOffset completedAt)
    {
        Status = InterviewStatus.Completed;
        CompletedAt = completedAt;
        UpdatedAt = completedAt;
    }

    public void RecordSummary(
        InterviewSummary interviewSummary,
        AiCallMetadata aiCallMetadata,
        DateTimeOffset updatedAt)
    {
        if (Status != InterviewStatus.Completed)
        {
            throw new DomainConflictException(Errors.SessionNotCompleted);
        }

        if (updatedAt < CreatedAt)
        {
            throw new InvalidOperationException("Updated timestamp cannot be before created timestamp.");
        }

        ArgumentNullException.ThrowIfNull(interviewSummary);
        ArgumentNullException.ThrowIfNull(aiCallMetadata);

        InterviewSummary = interviewSummary;
        SummaryMetadata = aiCallMetadata;
        UpdatedAt = updatedAt;
    }

    public static InterviewSession Restore(InterviewSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateRestoredState(state);

        return new InterviewSession
        {
            Id = state.Id,
            UserId = state.UserId,
            Status = state.Status,
            TargetRole = state.TargetRole,
            FocusArea = state.FocusArea,
            Seniority = state.Seniority,
            InterviewType = state.InterviewType,
            CreatedAt = state.CreatedAt,
            UpdatedAt = state.UpdatedAt,
            StartedAt = state.StartedAt,
            CompletedAt = state.CompletedAt,
            QuestionCount = state.QuestionCount,
            AnsweredCount = state.AnsweredCount,
            SessionResult = state.SessionResult,
            InterviewSummary = state.InterviewSummary,
            SummaryMetadata = state.SummaryMetadata,
            ConcurrencyToken = state.ConcurrencyToken
        };
    }

    public InterviewSessionState ToState() => new(
        Id: Id,
        UserId: UserId,
        Status: Status,
        TargetRole: TargetRole,
        FocusArea: FocusArea,
        Seniority: Seniority,
        InterviewType: InterviewType,
        CreatedAt: CreatedAt,
        UpdatedAt: UpdatedAt,
        StartedAt: StartedAt,
        CompletedAt: CompletedAt,
        QuestionCount: QuestionCount,
        AnsweredCount: AnsweredCount,
        SessionResult: SessionResult,
        InterviewSummary: InterviewSummary,
        SummaryMetadata: SummaryMetadata,
        ConcurrencyToken: ConcurrencyToken);

    private static void ValidateRestoredState(InterviewSessionState state)
    {
        if (state.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Persisted interview session id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(state.UserId))
        {
            throw new InvalidOperationException("Persisted interview session user id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(state.FocusArea))
        {
            throw new InvalidOperationException("Persisted interview focus area cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(state.TargetRole))
        {
            throw new InvalidOperationException("Persisted interview target role cannot be empty.");
        }

        if (!Enum.IsDefined(state.Status))
        {
            throw new InvalidOperationException("Persisted interview status is invalid.");
        }

        if (!Enum.IsDefined(state.Seniority))
        {
            throw new InvalidOperationException("Persisted interview seniority is invalid.");
        }

        if (!Enum.IsDefined(state.InterviewType))
        {
            throw new InvalidOperationException("Persisted interview type is invalid.");
        }

        if (state.QuestionCount <= 0)
        {
            throw new InvalidOperationException("Persisted interview question count must be greater than zero.");
        }

        if (state.AnsweredCount < 0)
        {
            throw new InvalidOperationException("Persisted answered count cannot be negative.");
        }

        if (state.AnsweredCount > state.QuestionCount)
        {
            throw new InvalidOperationException("Persisted answered count cannot exceed question count.");
        }

        if (state.AnsweredCount == 0 && state.SessionResult is not null)
        {
            throw new InvalidOperationException("Persisted session with no answered questions cannot have a result.");
        }

        if (state.UpdatedAt < state.CreatedAt)
        {
            throw new InvalidOperationException("Persisted updated timestamp cannot be before created timestamp.");
        }

        if ((state.Status is InterviewStatus.Active or InterviewStatus.Completed) &&
            state.StartedAt is null)
        {
            throw new InvalidOperationException("Active or completed interview must have StartedAt.");
        }

        if (state.Status == InterviewStatus.Completed &&
            state.CompletedAt is null)
        {
            throw new InvalidOperationException("Completed interview must have CompletedAt.");
        }

        if (state.Status != InterviewStatus.Completed &&
            state.CompletedAt is not null)
        {
            throw new InvalidOperationException("Only completed interview can have CompletedAt.");
        }

        if (state.Status == InterviewStatus.Created)
        {
            if (state.StartedAt is not null)
            {
                throw new InvalidOperationException("Created interview cannot have StartedAt.");
            }

            if (state.CompletedAt is not null)
            {
                throw new InvalidOperationException("Created interview cannot have CompletedAt.");
            }

            if (state.AnsweredCount != 0)
            {
                throw new InvalidOperationException("Created interview cannot have answered questions.");
            }
        }

        if (state.StartedAt is not null && state.StartedAt < state.CreatedAt)
        {
            throw new InvalidOperationException("Persisted started timestamp cannot be before created timestamp.");
        }

        if (state.CompletedAt is not null && state.CompletedAt < state.CreatedAt)
        {
            throw new InvalidOperationException("Persisted completed timestamp cannot be before created timestamp.");
        }

        if (state.StartedAt is not null &&
            state.CompletedAt is not null &&
            state.CompletedAt < state.StartedAt)
        {
            throw new InvalidOperationException("Persisted completed timestamp cannot be before started timestamp.");
        }
    }

    public static class Errors
    {
        public static DomainError SessionNotActive => new("Interviews.InterviewSession.SessionNotActive", "Interview session is not active.");
        public static DomainError SessionNotCreated => new("Interviews.InterviewSession.SessionNotCreated", "Interview session is not in a created state.");
        public static DomainError ActiveInterviewMissingStartedAt => new("Interviews.InterviewSession.ActiveInterviewMissingStartedAt", "Active interview must have a started timestamp.");
        public static DomainError CompletedBeforeStartedAt => new("Interviews.InterviewSession.CompletedBeforeStartedAt", "Completed timestamp cannot be before started timestamp.");
        public static DomainError AnsweredBeyondQuestionCount => new("Interviews.InterviewSession.AnsweredBeyondQuestionCount", "Cannot answer beyond the total question count.");
        public static DomainError AnsweredBeforeStartedAt => new("Interviews.InterviewSession.AnsweredBeforeStartedAt", "Answered timestamp cannot be before started timestamp.");
        public static DomainError SessionNotCompleted => new("Interviews.InterviewSession.SessionNotCompleted", "Cannot record summary for an interview session that is not completed.");
    }
}

public record SessionResult(Score OverallScore);

public record InterviewSummary(string Text, DateTimeOffset CreatedAt);

public enum InterviewStatus
{
    Created = 0,
    Active = 1,
    Completed = 2,
}

public enum InterviewType
{
    Technical = 1,
    Behavioral = 2,
    SystemDesign = 3,
}

public enum SeniorityLevel
{
    Junior = 1,
    Middle = 2,
    Senior = 3,
}

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

public sealed record InterviewSessionState(
    Guid Id,
    string UserId,
    InterviewStatus Status,
    string TargetRole,
    string FocusArea,
    SeniorityLevel Seniority,
    InterviewType InterviewType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int QuestionCount,
    int AnsweredCount,
    SessionResult? SessionResult,
    InterviewSummary? InterviewSummary,
    AiCallMetadata? SummaryMetadata,
    string? ConcurrencyToken = null);

