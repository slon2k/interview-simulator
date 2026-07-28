namespace InterviewSimulator.Api.Features.Interviews;

public interface IQuestionGenerator
{
    Task<GeneratedQuestion> GenerateQuestionAsync(
        GenerateQuestionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record GenerateQuestionRequest(
    string TargetRole,
    SeniorityLevel Seniority,
    InterviewType InterviewType,
    string FocusArea,
    int TurnNumber,
    int QuestionCount,
    IReadOnlyList<PreviousInterviewTurn> PreviousTurns);

public sealed record GeneratedQuestion(
    string Text,
    string Topic);

public sealed record PreviousInterviewTurn(
    int TurnNumber,
    string QuestionText,
    string QuestionTopic,
    string AnswerText);
