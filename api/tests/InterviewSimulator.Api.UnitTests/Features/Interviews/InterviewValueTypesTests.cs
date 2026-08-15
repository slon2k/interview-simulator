using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews;

public sealed class InterviewQuestion_Constructor
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesQuestion()
    {
        var text = "What is your experience with async programming?";
        var topic = "dotnet-async";

        var question = new InterviewQuestion(text: text, topic: topic);

        Assert.Equal(text, question.Text);
        Assert.Equal(topic, question.Topic);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidText_ThrowsArgumentException(string? text)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new InterviewQuestion(text: text!, topic: "topic"));

        Assert.Equal("text", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTopic_ThrowsArgumentException(string? topic)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new InterviewQuestion(text: "Question", topic: topic!));

        Assert.Equal("topic", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithLongText_Succeeds()
    {
        var text = new string('a', 1000);
        var topic = "topic";

        var question = new InterviewQuestion(text: text, topic: topic);

        Assert.Equal(text, question.Text);
    }
}

public sealed class InterviewAnswer_Constructor
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesAnswer()
    {
        var text = "I use async/await for non-blocking operations";
        var answeredAt = DateTimeOffset.UtcNow;

        var answer = new InterviewAnswer(text: text, answeredAt: answeredAt);

        Assert.Equal(text, answer.Text);
        Assert.Equal(answeredAt, answer.AnsweredAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidText_ThrowsArgumentException(string? text)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new InterviewAnswer(text: text!, answeredAt: DateTimeOffset.UtcNow));

        Assert.Equal("text", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithLongText_Succeeds()
    {
        var text = new string('a', 5000);
        var answeredAt = DateTimeOffset.UtcNow;

        var answer = new InterviewAnswer(text: text, answeredAt: answeredAt);

        Assert.Equal(text, answer.Text);
    }
}

public sealed class AnswerEvaluation_Constructor
{
    [Fact]
    public void Constructor_WithNullDimensions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AnswerEvaluation(new Score(75), new Feedback("Good"), null!));

        Assert.Equal("dimensions", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyDimensions_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new AnswerEvaluation(new Score(75), new Feedback("Good"), []));

        Assert.Equal("dimensions", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithValidArguments_CreatesEvaluation()
    {
        var score = 85;
        var feedback = "Great answer with good explanation.";

        var evaluation = new AnswerEvaluation(
            new Score(score),
            new Feedback(feedback),
            [new EvaluationDimension("clarity", "Clarity", new Score(score), new Feedback("Clear."))]);

        Assert.Equal(score, evaluation.OverallScore.Value);
        Assert.Equal(feedback, evaluation.Feedback.Text);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(200)]
    public void Constructor_WithInvalidScore_ThrowsArgumentOutOfRangeException(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnswerEvaluation(
                new Score(score),
                new Feedback("Feedback"),
                [new EvaluationDimension("key", "Label", new Score(50), new Feedback("Ok."))]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(50)]
    public void Constructor_WithBoundaryScores_Succeeds(int score)
    {
        var evaluation = new AnswerEvaluation(
            new Score(score),
            new Feedback("Feedback"),
            [new EvaluationDimension("key", "Label", new Score(score), new Feedback("Ok."))]);

        Assert.Equal(score, evaluation.OverallScore.Value);
    }

    [Fact]
    public void Constructor_WithLongFeedback_Succeeds()
    {
        var feedback = new string('a', 2000);

        var evaluation = new AnswerEvaluation(
            new Score(75),
            new Feedback(feedback),
            [new EvaluationDimension("key", "Label", new Score(75), new Feedback("Ok."))]);

        Assert.Equal(feedback, evaluation.Feedback.Text);
    }
}

public sealed class Feedback_Constructor
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesFeedback()
    {
        var totalScore = 82;

        var feedback = new SessionResult(new Score(totalScore));

        Assert.Equal(totalScore, feedback.OverallScore);
    }

    [Fact]
    public void Constructor_WithNullSummary_Succeeds()
    {
        var feedback = new SessionResult(new Score(75));

        Assert.Equal(75, feedback.OverallScore);
    }

    [Fact]
    public void Constructor_WithEmptySummary_Succeeds()
    {
        var feedback = new SessionResult(new Score(75));

        Assert.Equal(75, feedback.OverallScore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Constructor_WithVariousScores_Succeeds(int score)
    {
        var feedback = new SessionResult(new Score(score));

        Assert.Equal(score, feedback.OverallScore);
    }
}

public sealed class InterviewQuestion_Equality
{
    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var q1 = new InterviewQuestion(text: "Question?", topic: "topic");
        var q2 = new InterviewQuestion(text: "Question?", topic: "topic");

        Assert.Equal(q1, q2);
    }

    [Fact]
    public void RecordEquality_DifferentText_AreNotEqual()
    {
        var q1 = new InterviewQuestion(text: "Question 1?", topic: "topic");
        var q2 = new InterviewQuestion(text: "Question 2?", topic: "topic");

        Assert.NotEqual(q1, q2);
    }

    [Fact]
    public void RecordEquality_DifferentTopic_AreNotEqual()
    {
        var q1 = new InterviewQuestion(text: "Question?", topic: "topic1");
        var q2 = new InterviewQuestion(text: "Question?", topic: "topic2");

        Assert.NotEqual(q1, q2);
    }
}

public sealed class InterviewAnswer_Equality
{
    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var a1 = new InterviewAnswer(text: "Answer", answeredAt: now);
        var a2 = new InterviewAnswer(text: "Answer", answeredAt: now);

        Assert.Equal(a1, a2);
    }

    [Fact]
    public void RecordEquality_DifferentText_AreNotEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var a1 = new InterviewAnswer(text: "Answer 1", answeredAt: now);
        var a2 = new InterviewAnswer(text: "Answer 2", answeredAt: now);

        Assert.NotEqual(a1, a2);
    }

    [Fact]
    public void RecordEquality_DifferentTimestamp_AreNotEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var a1 = new InterviewAnswer(text: "Answer", answeredAt: now);
        var a2 = new InterviewAnswer(text: "Answer", answeredAt: now.AddSeconds(1));

        Assert.NotEqual(a1, a2);
    }
}

public sealed class AnswerEvaluation_Equality
{
    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        IReadOnlyList<EvaluationDimension> dims = [new EvaluationDimension("key", "Label", new Score(75), new Feedback("Good"))];
        var e1 = new AnswerEvaluation(new Score(75), new Feedback("Good"), dims);
        var e2 = new AnswerEvaluation(new Score(75), new Feedback("Good"), dims);

        Assert.Equal(e1, e2);
    }

    [Fact]
    public void RecordEquality_DifferentScore_AreNotEqual()
    {
        IReadOnlyList<EvaluationDimension> dims = [new EvaluationDimension("key", "Label", new Score(75), new Feedback("Good"))];
        var e1 = new AnswerEvaluation(new Score(75), new Feedback("Good"), dims);
        var e2 = new AnswerEvaluation(new Score(80), new Feedback("Good"), dims);

        Assert.NotEqual(e1, e2);
    }

    [Fact]
    public void RecordEquality_DifferentFeedback_AreNotEqual()
    {
        IReadOnlyList<EvaluationDimension> dims = [new EvaluationDimension("key", "Label", new Score(75), new Feedback("Good"))];
        var e1 = new AnswerEvaluation(new Score(75), new Feedback("Good"), dims);
        var e2 = new AnswerEvaluation(new Score(75), new Feedback("Very good"), dims);

        Assert.NotEqual(e1, e2);
    }
}

public sealed class EvaluationDimension_Constructor
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesDimension()
    {
        var dim = new EvaluationDimension("tech", "Technical Accuracy", new Score(80), new Feedback("Solid."));

        Assert.Equal("tech", dim.Key);
        Assert.Equal("Technical Accuracy", dim.Label);
        Assert.Equal(80, dim.Score.Value);
        Assert.Equal("Solid.", dim.Feedback.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidKey_ThrowsArgumentException(string? key)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new EvaluationDimension(key!, "Label", new Score(75), new Feedback("Good")));

        Assert.Equal("key", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidLabel_ThrowsArgumentException(string? label)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new EvaluationDimension("key", label!, new Score(75), new Feedback("Good")));

        Assert.Equal("label", ex.ParamName);
    }
}
