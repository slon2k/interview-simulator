namespace InterviewSimulator.Api.Features.Interviews;

public enum InterviewTypeContract
{
    Technical,
    Behavioral,
    SystemDesign,
}

public enum SeniorityLevelContract
{
    Junior,
    Middle,
    Senior,
}

public enum InterviewStatusContract
{
    Created,
    Active,
    Completed,
}

public record QuestionContract(string Text, string Topic, int TurnNumber);

public record FeedbackContract(int Score, string? Summary);

public record InterviewResponse(
    Guid Id,
    string UserId,
    InterviewStatusContract Status,
    string TargetRole,
    string FocusArea,
    InterviewTypeContract InterviewType,
    SeniorityLevelContract SeniorityLevel,
    int QuestionCount,
    int AnsweredCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    FeedbackContract? Feedback,
    QuestionContract? CurrentQuestion);
