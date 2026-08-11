using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Common;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews;

public sealed class InterviewTurn_Create
{
    [Fact]
    public void Create_WithValidArguments_CreatesTurnWithQuestionOnly()
    {
        var sessionId = Guid.NewGuid();
        var userId = "github|123456";
        var turnNumber = 1;
        var question = new InterviewQuestion("What is async/await?", "dotnet-async");
        var createdAt = DateTimeOffset.UtcNow;

        var turn = InterviewTurn.Create(
            sessionId: sessionId,
            userId: userId,
            turnNumber: turnNumber,
            question: question,
            questionGenerationMetadata: null,
            createdAt: createdAt);

        Assert.Equal(sessionId, turn.SessionId);
        Assert.Equal(userId, turn.UserId);
        Assert.Equal(turnNumber, turn.TurnNumber);
        Assert.Equal(question, turn.Question);
        Assert.Null(turn.Answer);
        Assert.Null(turn.Evaluation);
        Assert.Equal(createdAt, turn.CreatedAt);
        Assert.Equal(createdAt, turn.UpdatedAt);
        Assert.False(turn.IsAnswered);
        Assert.False(turn.IsEvaluated);
    }

    [Fact]
    public void Create_WithEmptySessionId_ThrowsArgumentException()
    {
        var question = new InterviewQuestion("What is async/await?", "dotnet-async");

        var ex = Assert.Throws<ArgumentException>(() =>
            InterviewTurn.Create(
                sessionId: Guid.Empty,
                userId: "user123",
                turnNumber: 1,
                question: question,
                questionGenerationMetadata: null,
                createdAt: DateTimeOffset.UtcNow));

        Assert.Equal("sessionId", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        var question = new InterviewQuestion("What is async/await?", "dotnet-async");

        var ex = Assert.Throws<ArgumentException>(() =>
            InterviewTurn.Create(
                sessionId: Guid.NewGuid(),
                userId: userId!,
                turnNumber: 1,
                question: question,
                questionGenerationMetadata: null,
                createdAt: DateTimeOffset.UtcNow));

        Assert.Equal("userId", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidTurnNumber_ThrowsArgumentOutOfRangeException(int turnNumber)
    {
        var question = new InterviewQuestion("What is async/await?", "dotnet-async");

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            InterviewTurn.Create(
                sessionId: Guid.NewGuid(),
                userId: "user123",
                turnNumber: turnNumber,
                question: question,
                questionGenerationMetadata: null,
                createdAt: DateTimeOffset.UtcNow));

        Assert.Equal("turnNumber", ex.ParamName);
    }

    [Fact]
    public void Create_WithNullQuestion_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            InterviewTurn.Create(
                sessionId: Guid.NewGuid(),
                userId: "user123",
                turnNumber: 1,
                question: null!,
                questionGenerationMetadata: null,
                createdAt: DateTimeOffset.UtcNow));

        Assert.Equal("question", ex.ParamName);
    }
}

public sealed class InterviewTurn_RecordAnswer
{
    [Fact]
    public void RecordAnswer_WithValidAnswer_RecordsAnswerAndUpdatesTimestamp()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        var answerText = "This is my answer";
        var answeredAt = createdAt.AddSeconds(10);
        turn.RecordAnswer(answerText, answeredAt);

        Assert.True(turn.IsAnswered);
        Assert.NotNull(turn.Answer);
        Assert.Equal(answerText, turn.Answer.Text);
        Assert.Equal(answeredAt, turn.Answer.AnsweredAt);
        Assert.Equal(answeredAt, turn.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordAnswer_WithInvalidAnswer_ThrowsArgumentException(string? answer)
    {
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: DateTimeOffset.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() =>
            turn.RecordAnswer(answer!, DateTimeOffset.UtcNow));

        Assert.Equal("answer", ex.ParamName);
    }

    [Fact]
    public void RecordAnswer_WhenAlreadyAnswered_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        turn.RecordAnswer("First answer", createdAt.AddSeconds(5));

        var ex = Assert.Throws<DomainConflictException>(() =>
            turn.RecordAnswer("Second answer", createdAt.AddSeconds(10)));

        Assert.Equal(InterviewTurn.Errors.TurnAlreadyAnswered.Code, ex.Code);
    }

    [Fact]
    public void RecordAnswer_BeforeCreated_ThrowsArgumentException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        var ex = Assert.Throws<ArgumentException>(() =>
            turn.RecordAnswer("Answer", createdAt.AddSeconds(-1)));

        Assert.Contains("cannot be before created", ex.Message);
    }
}

public sealed class InterviewTurn_RecordEvaluation
{
    [Fact]
    public void RecordEvaluation_OnAnsweredTurn_RecordsEvaluation()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        var answeredAt = createdAt.AddSeconds(5);
        turn.RecordAnswer("My answer", answeredAt);

        var evaluation = new AnswerEvaluation(
            new Score(75),
            new Feedback("Good work!"),
            [new EvaluationDimension("clarity", "Clarity", new Score(75), new Feedback("Clear."))]);
        var evaluatedAt = createdAt.AddSeconds(10);
        turn.RecordEvaluation(evaluation, null, evaluatedAt);

        Assert.True(turn.IsEvaluated);
        Assert.NotNull(turn.Evaluation);
        Assert.Equal(75, turn.Evaluation.OverallScore.Value);
        Assert.Equal("Good work!", turn.Evaluation.Feedback.Text);
        Assert.Equal(evaluatedAt, turn.UpdatedAt);
    }

    [Fact]
    public void RecordEvaluation_WithNullEvaluation_ThrowsArgumentNullException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        turn.RecordAnswer("Answer", createdAt.AddSeconds(5));

        var ex = Assert.Throws<ArgumentNullException>(() =>
            turn.RecordEvaluation(null!, null, createdAt.AddSeconds(10)));

        Assert.Equal("evaluation", ex.ParamName);
    }

    [Fact]
    public void RecordEvaluation_OnUnansweredTurn_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        var evaluation = new AnswerEvaluation(
            new Score(75),
            new Feedback("Good!"),
            [new EvaluationDimension("clarity", "Clarity", new Score(75), new Feedback("Clear."))]);
        var ex = Assert.Throws<DomainConflictException>(() =>
            turn.RecordEvaluation(evaluation, null, createdAt.AddSeconds(5)));

        Assert.Equal(InterviewTurn.Errors.CannotEvaluateUnansweredTurn.Code, ex.Code);
    }

    [Fact]
    public void RecordEvaluation_WhenAlreadyEvaluated_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        turn.RecordAnswer("Answer", createdAt.AddSeconds(5));
        turn.RecordEvaluation(
            new AnswerEvaluation(new Score(75), new Feedback("Good!"), [new EvaluationDimension("clarity", "Clarity", new Score(75), new Feedback("Clear."))]),
            null,
            createdAt.AddSeconds(10));

        var ex = Assert.Throws<DomainConflictException>(() =>
            turn.RecordEvaluation(
                new AnswerEvaluation(new Score(80), new Feedback("Better!"), [new EvaluationDimension("clarity", "Clarity", new Score(80), new Feedback("Clear."))]),
                null,
                createdAt.AddSeconds(15)));

        Assert.Equal(InterviewTurn.Errors.TurnAlreadyEvaluated.Code, ex.Code);
    }

    [Fact]
    public void RecordEvaluation_BeforeAnswered_ThrowsArgumentException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var answeredAt = createdAt.AddSeconds(5);
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        turn.RecordAnswer("Answer", answeredAt);

        var evaluation = new AnswerEvaluation(
            new Score(75),
            new Feedback("Good!"),
            [new EvaluationDimension("clarity", "Clarity", new Score(75), new Feedback("Clear."))]);
        var ex = Assert.Throws<ArgumentException>(() =>
            turn.RecordEvaluation(evaluation, null, answeredAt.AddSeconds(-1)));

        Assert.Contains("cannot be before answered", ex.Message);
    }
}

public sealed class InterviewTurn_StateProperties
{
    [Fact]
    public void IsAnswered_ReturnsTrueOnlyWhenAnswerRecorded()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        Assert.False(turn.IsAnswered);

        turn.RecordAnswer("Answer", createdAt.AddSeconds(5));

        Assert.True(turn.IsAnswered);
    }

    [Fact]
    public void IsEvaluated_ReturnsTrueOnlyWhenEvaluationRecorded()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        Assert.False(turn.IsEvaluated);

        turn.RecordAnswer("Answer", createdAt.AddSeconds(5));

        Assert.False(turn.IsEvaluated);

        turn.RecordEvaluation(
            new AnswerEvaluation(new Score(75), new Feedback("Good!"), [new EvaluationDimension("clarity", "Clarity", new Score(75), new Feedback("Clear."))]),
            null,
            createdAt.AddSeconds(10));

        Assert.True(turn.IsEvaluated);
    }
}
