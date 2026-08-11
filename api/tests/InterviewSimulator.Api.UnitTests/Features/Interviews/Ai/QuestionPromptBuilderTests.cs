using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews.Ai;

public sealed class QuestionPromptBuilderTests
{
    [Fact]
    public void BuildPrompt_IncludesCoreInterviewContext()
    {
        var options = CreateOptions();

        var prompt = PromptBuilder.BuildQuestionPrompt(
            targetRole: "Backend Engineer",
            seniority: SeniorityLevel.Senior,
            interviewType: InterviewType.Technical,
            focusArea: "dotnet",
            turnNumber: 2,
            questionCount: 5,
            previousTurns: [],
            options: options);

        Assert.Contains("Role: Backend Engineer", prompt, StringComparison.Ordinal);
        Assert.Contains("Seniority: senior", prompt, StringComparison.Ordinal);
        Assert.Contains("Interview type: technical", prompt, StringComparison.Ordinal);
        Assert.Contains("Focus area: dotnet", prompt, StringComparison.Ordinal);
        Assert.Contains("Turn: 2 of 5", prompt, StringComparison.Ordinal);
        Assert.Contains("Return only a valid JSON object", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPrompt_CapsPreviousTurnsToConfiguredMaximum()
    {
        var options = CreateOptions(maxPreviousTurns: 2);
        var turns = new[]
        {
            CreateTurn(1, "q1", "topic-1", "a1"),
            CreateTurn(2, "q2", "topic-2", "a2"),
            CreateTurn(3, "q3", "topic-3", "a3")
        };

        var prompt = PromptBuilder.BuildQuestionPrompt(
            targetRole: "Backend Engineer",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            focusArea: "dotnet",
            turnNumber: 4,
            questionCount: 5,
            previousTurns: turns,
            options: options);

        Assert.DoesNotContain("topic-1", prompt, StringComparison.Ordinal);
        Assert.Contains("topic-2", prompt, StringComparison.Ordinal);
        Assert.Contains("topic-3", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPrompt_TruncatesAnswerTextUsingConfiguredLimit()
    {
        var options = CreateOptions(maxAnswerChars: 5);
        var turns = new[]
        {
            CreateTurn(1, "question", "topic", "123456789")
        };

        var prompt = PromptBuilder.BuildQuestionPrompt(
            targetRole: "Backend Engineer",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            focusArea: "dotnet",
            turnNumber: 2,
            questionCount: 3,
            previousTurns: turns,
            options: options);

        Assert.Contains("Answer: 12345...", prompt, StringComparison.Ordinal);
    }

    private static AiOptions CreateOptions(int maxPreviousTurns = 3, int maxQuestionChars = 800, int maxAnswerChars = 1200)
    {
        return new AiOptions
        {
            MaxQuestionGenerationPreviousTurns = maxPreviousTurns,
            MaxQuestionChars = maxQuestionChars,
            MaxAnswerChars = maxAnswerChars,
        };
    }

    private static PreviousInterviewTurn CreateTurn(int number, string question, string topic, string answer)
    {
        return new PreviousInterviewTurn(number, question, topic, answer);
    }
}