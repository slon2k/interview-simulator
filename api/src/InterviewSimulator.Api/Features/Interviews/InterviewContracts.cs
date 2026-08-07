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

public record QuestionResponse(string Text, string Topic, int TurnNumber);

public record FeedbackResponse(int Score, string? Summary);
