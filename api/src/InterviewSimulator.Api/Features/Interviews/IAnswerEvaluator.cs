namespace InterviewSimulator.Api.Features.Interviews;

public interface IAnswerEvaluator
{
    Task<AnswerEvaluationResult> EvaluateAnswerAsync(
        EvaluateAnswerRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record EvaluateAnswerRequest(
    string TargetRole,
    SeniorityLevel Seniority,
    InterviewType InterviewType,
    string FocusArea,
    int TurnNumber,
    int QuestionCount,
    string QuestionText,
    string QuestionTopic,
    string AnswerText,
    IReadOnlyList<PreviousInterviewTurn> PreviousTurns);