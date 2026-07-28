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
    public void Constructor_WithValidArguments_CreatesEvaluation()
    {
        var score = 85;
        var feedback = "Great answer with good explanation.";

        var evaluation = new AnswerEvaluation(score: score, feedback: feedback);

        Assert.Equal(score, evaluation.Score);
        Assert.Equal(feedback, evaluation.Feedback);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(200)]
    public void Constructor_WithInvalidScore_ThrowsArgumentOutOfRangeException(int score)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnswerEvaluation(score: score, feedback: "Feedback"));

        Assert.Equal("score", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(50)]
    public void Constructor_WithBoundaryScores_Succeeds(int score)
    {
        var evaluation = new AnswerEvaluation(score: score, feedback: "Feedback");

        Assert.Equal(score, evaluation.Score);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFeedback_ThrowsArgumentException(string? feedback)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new AnswerEvaluation(score: 50, feedback: feedback!));

        Assert.Equal("feedback", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithLongFeedback_Succeeds()
    {
        var feedback = new string('a', 2000);

        var evaluation = new AnswerEvaluation(score: 75, feedback: feedback);

        Assert.Equal(feedback, evaluation.Feedback);
    }
}

public sealed class Feedback_Constructor
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesFeedback()
    {
        var totalScore = 82;
        var summary = "Overall good performance.";

        var feedback = new Feedback(TotalScore: totalScore, Summary: summary);

        Assert.Equal(totalScore, feedback.TotalScore);
        Assert.Equal(summary, feedback.Summary);
    }

    [Fact]
    public void Constructor_WithNullSummary_Succeeds()
    {
        var feedback = new Feedback(TotalScore: 75, Summary: null);

        Assert.Equal(75, feedback.TotalScore);
        Assert.Null(feedback.Summary);
    }

    [Fact]
    public void Constructor_WithEmptySummary_Succeeds()
    {
        var feedback = new Feedback(TotalScore: 75, Summary: "");

        Assert.Equal(75, feedback.TotalScore);
        Assert.Equal("", feedback.Summary);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Constructor_WithVariousScores_Succeeds(int score)
    {
        var feedback = new Feedback(TotalScore: score, Summary: "Summary");

        Assert.Equal(score, feedback.TotalScore);
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
        var e1 = new AnswerEvaluation(score: 75, feedback: "Good");
        var e2 = new AnswerEvaluation(score: 75, feedback: "Good");

        Assert.Equal(e1, e2);
    }

    [Fact]
    public void RecordEquality_DifferentScore_AreNotEqual()
    {
        var e1 = new AnswerEvaluation(score: 75, feedback: "Good");
        var e2 = new AnswerEvaluation(score: 80, feedback: "Good");

        Assert.NotEqual(e1, e2);
    }

    [Fact]
    public void RecordEquality_DifferentFeedback_AreNotEqual()
    {
        var e1 = new AnswerEvaluation(score: 75, feedback: "Good");
        var e2 = new AnswerEvaluation(score: 75, feedback: "Very good");

        Assert.NotEqual(e1, e2);
    }
}
