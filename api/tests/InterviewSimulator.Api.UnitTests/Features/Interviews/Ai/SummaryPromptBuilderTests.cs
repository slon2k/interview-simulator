using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews.Ai;

public sealed class SummaryPromptBuilderTests
{
    [Fact]
    public void BuildPrompt_IncludesCoreInterviewContextAndEvaluationSummary()
    {
        var options = CreateOptions();
        var turns = new[]
        {
            CreateTurn(1, "q1", "topic-1", "a1", 80, "good"),
            CreateTurn(2, "q2", "topic-2", "a2", 60, "mixed")
        };

        var prompt = PromptBuilder.BuildSessionSummaryPrompt(
            targetRole: "Backend Engineer",
            seniority: SeniorityLevel.Senior,
            interviewType: InterviewType.Technical,
            focusArea: "dotnet",
            turns: turns,
            options: options);

        Assert.Contains("Role: Backend Engineer", prompt, StringComparison.Ordinal);
        Assert.Contains("Seniority: senior", prompt, StringComparison.Ordinal);
        Assert.Contains("Interview type: technical", prompt, StringComparison.Ordinal);
        Assert.Contains("Focus area: dotnet", prompt, StringComparison.Ordinal);
        Assert.Contains("Turn 1", prompt, StringComparison.Ordinal);
        Assert.Contains("topic-1", prompt, StringComparison.Ordinal);
        Assert.Contains("Overall score: 70", prompt, StringComparison.Ordinal);
        Assert.Contains("Feedback: good", prompt, StringComparison.Ordinal);
        Assert.Contains("Return only a valid JSON object", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPrompt_UsesConfiguredTurnLimitForSummaries()
    {
        var options = CreateOptions(maxPreviousTurns: 1);
        var turns = new[]
        {
            CreateTurn(1, "q1", "topic-1", "a1", 80, "good"),
            CreateTurn(2, "q2", "topic-2", "a2", 60, "mixed")
        };

        var prompt = PromptBuilder.BuildSessionSummaryPrompt(
            targetRole: "Backend Engineer",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Behavioral,
            focusArea: "leadership",
            turns: turns,
            options: options);

        Assert.DoesNotContain("topic-1", prompt, StringComparison.Ordinal);
        Assert.Contains("topic-2", prompt, StringComparison.Ordinal);
    }

    private static AiOptions CreateOptions(int maxPreviousTurns = 3)
    {
        return new AiOptions
        {
            MaxQuestionGenerationPreviousTurns = maxPreviousTurns,
            MaxQuestionChars = 800,
            MaxAnswerChars = 1200,
            MaxFeedbackChars = 400,
        };
    }

    private static SessionSummaryTurn CreateTurn(int number, string question, string topic, string answer, int score, string feedback)
    {
        return new SessionSummaryTurn(
            TurnNumber: number,
            QuestionText: question,
            QuestionTopic: topic,
            AnswerText: answer,
            OverallScore: score,
            Feedback: feedback);
    }
}
