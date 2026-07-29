using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews;

public sealed class HardcodedQuestionGenerator_GenerateQuestionAsync
{
    [Fact]
    public async Task GenerateQuestionAsync_WithSameInput_ReturnsSameOutput()
    {
        var generator = new HardcodedQuestionGenerator();
        var request = CreateRequest(turnNumber: 1);

        var first = await generator.GenerateQuestionAsync(request);
        var second = await generator.GenerateQuestionAsync(request);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GenerateQuestionAsync_WithDifferentTurnNumber_CyclesTemplatesDeterministically()
    {
        var generator = new HardcodedQuestionGenerator();

        var turn1 = await generator.GenerateQuestionAsync(CreateRequest(turnNumber: 1));
        var turn2 = await generator.GenerateQuestionAsync(CreateRequest(turnNumber: 2));
        var turn3 = await generator.GenerateQuestionAsync(CreateRequest(turnNumber: 3));
        var turn4 = await generator.GenerateQuestionAsync(CreateRequest(turnNumber: 4));

        Assert.NotEqual(turn1.Text, turn2.Text);
        Assert.NotEqual(turn2.Text, turn3.Text);
        Assert.Equal(turn1.Text, turn4.Text);
    }

    [Fact]
    public async Task GenerateQuestionAsync_WithSecondTurnAndHistory_AddsFollowUpPrefix()
    {
        var generator = new HardcodedQuestionGenerator();
        var request = CreateRequest(
            turnNumber: 2,
            previousTurns:
            [
                new PreviousInterviewTurn(
                    TurnNumber: 1,
                    QuestionText: "Initial question",
                    QuestionTopic: "technical/dotnet/t1",
                    AnswerText: "My answer")
            ]);

        var result = await generator.GenerateQuestionAsync(request);

        Assert.StartsWith("Building on your previous answer about technical/dotnet/t1: ", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateQuestionAsync_IncludesRoleSeniorityAndFocusArea()
    {
        var generator = new HardcodedQuestionGenerator();
        var request = CreateRequest(
            turnNumber: 1,
            targetRole: "Backend Engineer",
            focusArea: "Distributed Caching",
            seniority: SeniorityLevel.Senior);

        var result = await generator.GenerateQuestionAsync(request);

        Assert.Contains("senior", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Backend Engineer", result.Text, StringComparison.Ordinal);
        Assert.Contains("Distributed Caching", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(InterviewType.Technical)]
    [InlineData(InterviewType.Behavioral)]
    [InlineData(InterviewType.SystemDesign)]
    public async Task GenerateQuestionAsync_UsesFocusAreaAsTopic(InterviewType interviewType)
    {
        var generator = new HardcodedQuestionGenerator();
        var request = CreateRequest(turnNumber: 2, interviewType: interviewType);

        var result = await generator.GenerateQuestionAsync(request);

        Assert.Equal("dotnet", result.Topic);
    }

    private static GenerateQuestionRequest CreateRequest(
        int turnNumber,
        string targetRole = "Software Engineer",
        SeniorityLevel seniority = SeniorityLevel.Middle,
        InterviewType interviewType = InterviewType.Technical,
        string focusArea = "dotnet",
        int questionCount = 5,
        IReadOnlyList<PreviousInterviewTurn>? previousTurns = null)
    {
        return new GenerateQuestionRequest(
            TargetRole: targetRole,
            Seniority: seniority,
            InterviewType: interviewType,
            FocusArea: focusArea,
            TurnNumber: turnNumber,
            QuestionCount: questionCount,
            PreviousTurns: previousTurns ?? []);
    }
}
