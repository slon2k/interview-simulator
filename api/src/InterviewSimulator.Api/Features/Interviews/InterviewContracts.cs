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

public record Question(string Text, string Topic);

public record Feedback(int Score, string? Summary);