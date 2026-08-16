using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews;

public sealed class GetInterviewDetailsMappingTests
{
    [Fact]
    public void ResponseTurn_FromDomain_MapsQuestionAnswerEvaluationAndDimensionsForCompletedSession()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = CreateTurn(createdAt, answered: true, evaluated: true);

        var result = GetInterviewDetails.ResponseTurn.FromDomain(turn, InterviewStatus.Completed);

        Assert.Equal(1, result.TurnNumber);
        Assert.Equal("Question", result.Question.Text);
        Assert.Equal("Topic", result.Question.Topic);
        Assert.Equal("Answer", result.Answer!.Text);
        Assert.NotNull(result.Evaluation);
        Assert.Equal(80, result.Evaluation.OverallScore);
        Assert.Equal("Good answer.", result.Evaluation.OverallFeedback);
        var dimension = Assert.Single(result.Evaluation.Dimensions);
        Assert.Equal("clarity", dimension.Key);
        Assert.Equal("Clarity", dimension.Label);
        Assert.Equal(75, dimension.Score);
        Assert.Equal("Clear.", dimension.Feedback);
    }

    [Fact]
    public void ResponseTurn_FromDomain_HidesEvaluationForActiveSession()
    {
        var turn = CreateTurn(DateTimeOffset.UtcNow, answered: true, evaluated: true);

        var result = GetInterviewDetails.ResponseTurn.FromDomain(turn, InterviewStatus.Active);

        Assert.NotNull(result.Answer);
        Assert.Null(result.Evaluation);
    }

    [Fact]
    public void ResponseTurn_FromDomain_MapsNullAnswerAndEvaluation()
    {
        var result = GetInterviewDetails.ResponseTurn.FromDomain(
            CreateTurn(DateTimeOffset.UtcNow, answered: false, evaluated: false),
            InterviewStatus.Completed);

        Assert.Null(result.Answer);
        Assert.Null(result.Evaluation);
    }

    [Fact]
    public void ResponseSummary_FromDomain_MapsSummary()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var result = GetInterviewDetails.ResponseSummary.FromDomain(new InterviewSummary("Summary text", createdAt));

        Assert.NotNull(result);
        Assert.Equal("Summary text", result.Text);
        Assert.Equal(createdAt, result.CreatedAt);
    }

    [Fact]
    public void ResponseSummary_FromDomain_ReturnsNullWhenSummaryIsMissing()
    {
        Assert.Null(GetInterviewDetails.ResponseSummary.FromDomain(null));
    }

    private static InterviewTurn CreateTurn(DateTimeOffset createdAt, bool answered, bool evaluated)
    {
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "github|100",
            turnNumber: 1,
            question: new InterviewQuestion("Question", "Topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        if (answered)
        {
            turn.RecordAnswer("Answer", createdAt.AddSeconds(1));
        }

        if (evaluated)
        {
            turn.RecordEvaluation(
                new AnswerEvaluation(
                    new Score(80),
                    new Feedback("Good answer."),
                    [new EvaluationDimension("clarity", "Clarity", new Score(75), new Feedback("Clear."))]),
                metadata: null,
                updatedAt: createdAt.AddSeconds(2));
        }

        return turn;
    }
}
