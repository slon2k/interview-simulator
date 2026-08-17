using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews.Ai;

public sealed class AnswerEvaluationPromptBuilderTests
{
    [Fact]
    public void BuildPrompt_ExplicitlyLimitsEveryFeedbackValue()
    {
        var options = CreateOptions();
        var rubric = EvaluationRubrics.GetForInterviewType(InterviewType.Technical);

        var prompt = PromptBuilder.BuildAnswerEvaluationPrompt(
            targetRole: "Backend Engineer",
            seniority: SeniorityLevel.Senior,
            interviewType: InterviewType.Technical,
            focusArea: "dotnet",
            turnNumber: 1,
            questionCount: 3,
            questionText: "How do you handle cancellation?",
            questionTopic: "async",
            answerText: "I use cancellation tokens.",
            previousTurns: [],
            rubric: rubric,
            options: options);

        Assert.Contains("Every feedback value, including overall feedback and dimension feedback, must be no more than 600 characters", prompt, StringComparison.Ordinal);
        Assert.Contains("Finish the complete JSON object before stopping", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_RejectsDimensionFeedbackOverConfiguredLimit()
    {
        var options = CreateOptions();
        var validator = new AnswerEvaluationResponseValidator(Microsoft.Extensions.Options.Options.Create(options));
        var response = new AnswerEvaluationResponse(
            Dimensions: [new AnswerEvaluationResponseDimension(
                Key: "quality",
                Score: 80,
                Feedback: new string('x', options.MaxFeedbackChars + 1))],
            Feedback: "Overall feedback");

        var result = validator.Validate(response);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains($"dimension feedback must be at most {options.MaxFeedbackChars}", StringComparison.Ordinal));
    }

    private static AiOptions CreateOptions() => new()
    {
        MaxQuestionChars = 600,
        MaxAnswerChars = 4000,
        MaxFeedbackChars = 600,
        MaxEvaluationPreviousTurns = 2,
    };
}
