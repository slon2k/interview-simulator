using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Infrastructure.Interviews;

namespace InterviewSimulator.Api.UnitTests.Infrastructure.Interviews;

public sealed class CosmosTurnDocument_Mapping
{
    [Fact]
    public void FromDomain_WithConcurrencyToken_MapsEtag()
    {
        var now = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Restore(new InterviewTurnState(
            SessionId: Guid.NewGuid(),
            UserId: "user123",
            TurnNumber: 1,
            Question: new InterviewQuestion("Question", "topic"),
            Answer: null,
            Evaluation: null,
            CreatedAt: now,
            UpdatedAt: now,
            ConcurrencyToken: "etag-turn-1"));

        var doc = CosmosTurnDocument.FromDomain(turn);

        Assert.Equal("etag-turn-1", doc.ETag);
    }

    [Fact]
    public void ToDomain_WithEtag_MapsConcurrencyToken()
    {
        var sessionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var doc = new CosmosTurnDocument
        {
            Id = $"turn|{sessionId}|001",
            UserId = "user123",
            SessionId = sessionId.ToString(),
            TurnNumber = 1,
            Question = new CosmosQuestionDocument
            {
                Text = "Question",
                Topic = "topic"
            },
            CreatedAt = now,
            UpdatedAt = now,
            ETag = "etag-turn-2"
        };

        var turn = doc.ToDomain();

        Assert.Equal("etag-turn-2", turn.ConcurrencyToken);
    }

    [Fact]
    public void FromDomain_WithCompleteTurn_MapsAllFields()
    {
        var sessionId = Guid.NewGuid();
        var userId = "user123";
        var createdAt = DateTimeOffset.UtcNow;
        var answeredAt = createdAt.AddSeconds(30);
        var evaluatedAt = createdAt.AddSeconds(60);

        var turn = InterviewTurn.Create(
            sessionId: sessionId,
            userId: userId,
            turnNumber: 2,
            question: new InterviewQuestion("Why use microservices?", "architecture"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        turn.RecordAnswer("Microservices allow independent scaling...", answeredAt);
        turn.RecordEvaluation(
            new AnswerEvaluation(
                new Score(88),
                new Feedback("Good understanding"),
                [new EvaluationDimension("depth", "Depth", new Score(88), new Feedback("Thorough."))]),
            null,
            evaluatedAt);

        var doc = CosmosTurnDocument.FromDomain(turn);

        Assert.Equal($"turn|{sessionId}|002", doc.Id);
        Assert.Equal(userId, doc.UserId);
        Assert.Equal(sessionId.ToString(), doc.SessionId);
        Assert.Equal(2, doc.TurnNumber);
        Assert.Equal("turn", doc.Type);
        Assert.Equal(1, doc.SchemaVersion);
        Assert.Equal("Why use microservices?", doc.Question.Text);
        Assert.Equal("architecture", doc.Question.Topic);
        Assert.NotNull(doc.Answer);
        Assert.Equal("Microservices allow independent scaling...", doc.Answer.Text);
        Assert.Equal(answeredAt, doc.AnsweredAt);
        Assert.NotNull(doc.Evaluation);
        Assert.Equal(88, doc.Evaluation.OverallScore);
        Assert.Equal("Good understanding", doc.Evaluation.Feedback);
        Assert.Equal(createdAt, doc.CreatedAt);
        Assert.Equal(evaluatedAt, doc.UpdatedAt);
    }

    [Fact]
    public void ToDomain_WithCompleteDocument_RestoresAllFields()
    {
        var sessionId = Guid.NewGuid();
        var userId = "user123";
        var createdAt = DateTimeOffset.UtcNow;
        var answeredAt = createdAt.AddSeconds(30);
        var evaluatedAt = createdAt.AddSeconds(60);

        var doc = new CosmosTurnDocument
        {
            Id = $"turn|{sessionId}|002",
            UserId = userId,
            SessionId = sessionId.ToString(),
            TurnNumber = 2,
            Type = "turn",
            SchemaVersion = 1,
            Question = new CosmosQuestionDocument
            {
                Text = "Why use microservices?",
                Topic = "architecture"
            },
            Answer = new CosmosAnswerDocument
            {
                Text = "Microservices allow independent scaling..."
            },
            Evaluation = new CosmosEvaluationDocument
            {
                OverallScore = 88,
                Feedback = "Good understanding",
                Dimensions =
                [
                    new CosmosEvaluationDimensionDocument
                    {
                        Key = "depth",
                        Label = "Depth",
                        Score = 88,
                        Feedback = "Thorough."
                    }
                ]
            },
            AnsweredAt = answeredAt,
            CreatedAt = createdAt,
            UpdatedAt = evaluatedAt
        };

        var turn = doc.ToDomain();

        Assert.Equal(sessionId, turn.SessionId);
        Assert.Equal(userId, turn.UserId);
        Assert.Equal(2, turn.TurnNumber);
        Assert.Equal("Why use microservices?", turn.Question.Text);
        Assert.Equal("architecture", turn.Question.Topic);
        Assert.True(turn.IsAnswered);
        Assert.Equal("Microservices allow independent scaling...", turn.Answer!.Text);
        Assert.Equal(answeredAt, turn.Answer.AnsweredAt);
        Assert.True(turn.IsEvaluated);
        Assert.Equal(88, turn.Evaluation!.OverallScore.Value);
        Assert.Equal("Good understanding", turn.Evaluation.Feedback.Text);
        Assert.Equal(createdAt, turn.CreatedAt);
        Assert.Equal(evaluatedAt, turn.UpdatedAt);
    }

    [Fact]
    public void RoundTrip_Turn_PreservesAllState()
    {
        var sessionId = Guid.NewGuid();
        var userId = "user123";
        var createdAt = DateTimeOffset.UtcNow;

        var originalTurn = InterviewTurn.Create(
            sessionId: sessionId,
            userId: userId,
            turnNumber: 3,
            question: new InterviewQuestion("Describe your approach to testing", "testing"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        originalTurn.RecordAnswer("I use TDD with xUnit", createdAt.AddSeconds(45));
        originalTurn.RecordEvaluation(
            new AnswerEvaluation(
                new Score(92),
                new Feedback("Excellent testing practices"),
                [new EvaluationDimension("depth", "Depth", new Score(92), new Feedback("Thorough."))]),
            null,
            createdAt.AddSeconds(90));

        var doc = CosmosTurnDocument.FromDomain(originalTurn);
        var restoredTurn = doc.ToDomain();

        Assert.Equal(originalTurn.SessionId, restoredTurn.SessionId);
        Assert.Equal(originalTurn.UserId, restoredTurn.UserId);
        Assert.Equal(originalTurn.TurnNumber, restoredTurn.TurnNumber);
        Assert.Equal(originalTurn.Question.Text, restoredTurn.Question.Text);
        Assert.Equal(originalTurn.Answer?.Text, restoredTurn.Answer?.Text);
        Assert.Equal(originalTurn.Evaluation?.OverallScore, restoredTurn.Evaluation?.OverallScore);
        Assert.Equal(originalTurn.Evaluation?.Feedback, restoredTurn.Evaluation?.Feedback);
    }

    [Fact]
    public void FromDomain_WithoutAnswer_MapsCorrectly()
    {
        var sessionId = Guid.NewGuid();
        var turn = InterviewTurn.Create(
            sessionId: sessionId,
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: DateTimeOffset.UtcNow);

        var doc = CosmosTurnDocument.FromDomain(turn);

        Assert.Null(doc.Answer);
        Assert.Null(doc.Evaluation);
    }

    [Fact]
    public void ToDomain_WithoutAnswer_MapsCorrectly()
    {
        var sessionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var doc = new CosmosTurnDocument
        {
            Id = $"turn|{sessionId}|001",
            UserId = "user123",
            SessionId = sessionId.ToString(),
            TurnNumber = 1,
            Type = "turn",
            SchemaVersion = 1,
            Question = new CosmosQuestionDocument
            {
                Text = "Question?",
                Topic = "topic"
            },
            Answer = null,
            Evaluation = null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        var turn = doc.ToDomain();

        Assert.False(turn.IsAnswered);
        Assert.False(turn.IsEvaluated);
    }

    [Fact]
    public void IdFormatting_WithDifferentTurnNumbers_FormatsCorrectly()
    {
        var sessionId = Guid.NewGuid();
        var userId = "user123";

        // Test with turnNumber 1
        var turn1 = InterviewTurn.Create(
            sessionId: sessionId,
            userId: userId,
            turnNumber: 1,
            question: new InterviewQuestion("Q1", "t1"),
            questionGenerationMetadata: null,
            createdAt: DateTimeOffset.UtcNow);

        var doc1 = CosmosTurnDocument.FromDomain(turn1);
        Assert.Equal($"turn|{sessionId}|001", doc1.Id);

        // Test with turnNumber 10
        var turn10 = InterviewTurn.Create(
            sessionId: sessionId,
            userId: userId,
            turnNumber: 10,
            question: new InterviewQuestion("Q10", "t10"),
            questionGenerationMetadata: null,
            createdAt: DateTimeOffset.UtcNow);

        var doc10 = CosmosTurnDocument.FromDomain(turn10);
        Assert.Equal($"turn|{sessionId}|010", doc10.Id);

        // Test with turnNumber 100
        var turn100 = InterviewTurn.Create(
            sessionId: sessionId,
            userId: userId,
            turnNumber: 100,
            question: new InterviewQuestion("Q100", "t100"),
            questionGenerationMetadata: null,
            createdAt: DateTimeOffset.UtcNow);

        var doc100 = CosmosTurnDocument.FromDomain(turn100);
        Assert.Equal($"turn|{sessionId}|100", doc100.Id);

        // Verify alphabetical ordering: 001 < 010 < 100
        var ids = new[] { doc1.Id, doc10.Id, doc100.Id }.OrderBy(x => x).ToList();
        Assert.Equal(doc1.Id, ids[0]);
        Assert.Equal(doc10.Id, ids[1]);
        Assert.Equal(doc100.Id, ids[2]);
    }

    [Fact]
    public void FromDomain_WithPartialAnswer_MapsCorrectly()
    {
        var sessionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var answeredAt = createdAt.AddSeconds(30);

        var turn = InterviewTurn.Create(
            sessionId: sessionId,
            userId: "user123",
            turnNumber: 2,
            question: new InterviewQuestion("Question?", "topic"),
            questionGenerationMetadata: null,
            createdAt: createdAt);

        turn.RecordAnswer("My answer", answeredAt);
        // Note: No evaluation yet

        var doc = CosmosTurnDocument.FromDomain(turn);

        Assert.NotNull(doc.Answer);
        Assert.Equal("My answer", doc.Answer.Text);
        Assert.Null(doc.Evaluation);
    }

    [Fact]
    public void ToDomain_WithPartialAnswer_MapsCorrectly()
    {
        var sessionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var answeredAt = createdAt.AddSeconds(30);

        var doc = new CosmosTurnDocument
        {
            Id = $"turn|{sessionId}|002",
            UserId = "user123",
            SessionId = sessionId.ToString(),
            TurnNumber = 2,
            Type = "turn",
            SchemaVersion = 1,
            Question = new CosmosQuestionDocument
            {
                Text = "Question?",
                Topic = "topic"
            },
            Answer = new CosmosAnswerDocument
            {
                Text = "My answer"
            },
            Evaluation = null,
            AnsweredAt = answeredAt,
            CreatedAt = createdAt,
            UpdatedAt = answeredAt
        };

        var turn = doc.ToDomain();

        Assert.True(turn.IsAnswered);
        Assert.False(turn.IsEvaluated);
        Assert.Equal("My answer", turn.Answer!.Text);
    }
}

public sealed class CosmosDocumentIds
{
    [Fact]
    public void SessionDocumentId_Format_IsCorrect()
    {
        var sessionId = Guid.NewGuid();
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: DateTimeOffset.UtcNow,
            questionCount: 5);

        var restoredSession = InterviewSession.Restore(new InterviewSessionState(
            Id: sessionId,
            UserId: "user123",
            Status: InterviewStatus.Created,
            TargetRole: "role",
            FocusArea: "area",
            Seniority: SeniorityLevel.Junior,
            InterviewType: InterviewType.Technical,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            StartedAt: null,
            CompletedAt: null,
            QuestionCount: 5,
            AnsweredCount: 0,
            Feedback: null));

        var doc = CosmosSessionDocument.FromDomain(restoredSession);

        Assert.Equal($"session|{sessionId}", doc.Id);
        Assert.StartsWith("session|", doc.Id);
    }

    [Fact]
    public void TurnDocumentId_Format_IncludesSessionIdAndTurnNumber()
    {
        var sessionId = Guid.NewGuid();
        var turnNumber = 5;

        var turn = InterviewTurn.Create(
            sessionId: sessionId,
            userId: "user123",
            turnNumber: turnNumber,
            question: new InterviewQuestion("Q", "t"),
            questionGenerationMetadata: null,
            createdAt: DateTimeOffset.UtcNow);

        var doc = CosmosTurnDocument.FromDomain(turn);

        Assert.Equal($"turn|{sessionId}|005", doc.Id);
    }
}
