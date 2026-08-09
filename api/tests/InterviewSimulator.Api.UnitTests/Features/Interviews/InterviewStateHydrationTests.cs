using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews;

public sealed class InterviewSession_StateHydration
{
    [Fact]
    public void ToState_ReturnCompleteState()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 5);

        session.Start(createdAt.AddSeconds(1));
        session.RecordAnswer(createdAt.AddSeconds(2));
        session.RecordAnswer(createdAt.AddSeconds(3));

        var state = session.ToState();

        Assert.Equal(session.Id, state.Id);
        Assert.Equal(session.UserId, state.UserId);
        Assert.Equal(session.Status, state.Status);
        Assert.Equal(session.TargetRole, state.TargetRole);
        Assert.Equal(session.FocusArea, state.FocusArea);
        Assert.Equal(session.Seniority, state.Seniority);
        Assert.Equal(session.InterviewType, state.InterviewType);
        Assert.Equal(session.CreatedAt, state.CreatedAt);
        Assert.Equal(session.UpdatedAt, state.UpdatedAt);
        Assert.Equal(session.StartedAt, state.StartedAt);
        Assert.Equal(session.CompletedAt, state.CompletedAt);
        Assert.Equal(session.QuestionCount, state.QuestionCount);
        Assert.Equal(session.AnsweredCount, state.AnsweredCount);
        Assert.Equal(session.Feedback, state.Feedback);
    }

    [Fact]
    public void Restore_WithValidState_RehydratesSession()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var startedAt = createdAt.AddSeconds(1);
        var id = Guid.NewGuid();

        var state = new InterviewSessionState(
            Id: id,
            UserId: "user123",
            Status: InterviewStatus.Active,
            TargetRole: "role",
            FocusArea: "area",
            Seniority: SeniorityLevel.Middle,
            InterviewType: InterviewType.Behavioral,
            CreatedAt: createdAt,
            UpdatedAt: startedAt,
            StartedAt: startedAt,
            CompletedAt: null,
            QuestionCount: 3,
            AnsweredCount: 1,
            Feedback: null);

        var session = InterviewSession.Restore(state);

        Assert.Equal(id, session.Id);
        Assert.Equal("user123", session.UserId);
        Assert.Equal(InterviewStatus.Active, session.Status);
        Assert.Equal("role", session.TargetRole);
        Assert.Equal("area", session.FocusArea);
        Assert.Equal(SeniorityLevel.Middle, session.Seniority);
        Assert.Equal(InterviewType.Behavioral, session.InterviewType);
        Assert.Equal(createdAt, session.CreatedAt);
        Assert.Equal(startedAt, session.UpdatedAt);
        Assert.Equal(startedAt, session.StartedAt);
        Assert.Null(session.CompletedAt);
        Assert.Equal(3, session.QuestionCount);
        Assert.Equal(1, session.AnsweredCount);
        Assert.Null(session.Feedback);
    }

    [Fact]
    public void Restore_RoundTrip_PreservesState()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session1 = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Senior,
            interviewType: InterviewType.SystemDesign,
            createdAt: createdAt,
            questionCount: 10);

        session1.Start(createdAt.AddSeconds(5));
        for (int i = 0; i < 3; i++)
        {
            session1.RecordAnswer(createdAt.AddSeconds(6 + i));
        }

        var state = session1.ToState();
        var session2 = InterviewSession.Restore(state);
        var state2 = session2.ToState();

        Assert.Equal(state, state2);
    }

    [Fact]
    public void Restore_WithEmptyId_ThrowsInvalidOperationException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var state = new InterviewSessionState(
            Id: Guid.Empty,
            UserId: "user123",
            Status: InterviewStatus.Created,
            TargetRole: "role",
            FocusArea: "area",
            Seniority: SeniorityLevel.Junior,
            InterviewType: InterviewType.Technical,
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            StartedAt: null,
            CompletedAt: null,
            QuestionCount: 5,
            AnsweredCount: 0,
            Feedback: null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InterviewSession.Restore(state));

        Assert.Contains("cannot be empty", ex.Message);
    }

    [Fact]
    public void Restore_WithEmptyUserId_ThrowsInvalidOperationException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var state = new InterviewSessionState(
            Id: Guid.NewGuid(),
            UserId: "",
            Status: InterviewStatus.Created,
            TargetRole: "role",
            FocusArea: "area",
            Seniority: SeniorityLevel.Junior,
            InterviewType: InterviewType.Technical,
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            StartedAt: null,
            CompletedAt: null,
            QuestionCount: 5,
            AnsweredCount: 0,
            Feedback: null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InterviewSession.Restore(state));

        Assert.Contains("user id cannot be empty", ex.Message);
    }

    [Fact]
    public void Restore_WithAnsweredCountExceedingQuestionCount_ThrowsInvalidOperationException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var state = new InterviewSessionState(
            Id: Guid.NewGuid(),
            UserId: "user123",
            Status: InterviewStatus.Active,
            TargetRole: "role",
            FocusArea: "area",
            Seniority: SeniorityLevel.Junior,
            InterviewType: InterviewType.Technical,
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            StartedAt: createdAt.AddSeconds(1),
            CompletedAt: null,
            QuestionCount: 5,
            AnsweredCount: 10,
            Feedback: null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InterviewSession.Restore(state));

        Assert.Contains("cannot exceed question count", ex.Message);
    }

    [Fact]
    public void Restore_CreatedSessionWithStartedAt_ThrowsInvalidOperationException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var state = new InterviewSessionState(
            Id: Guid.NewGuid(),
            UserId: "user123",
            Status: InterviewStatus.Created,
            TargetRole: "role",
            FocusArea: "area",
            Seniority: SeniorityLevel.Junior,
            InterviewType: InterviewType.Technical,
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            StartedAt: createdAt.AddSeconds(1), // Invalid for Created status
            CompletedAt: null,
            QuestionCount: 5,
            AnsweredCount: 0,
            Feedback: null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InterviewSession.Restore(state));

        Assert.Contains("cannot have StartedAt", ex.Message);
    }

    [Fact]
    public void Restore_CompletedWithoutStartedAt_ThrowsInvalidOperationException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var state = new InterviewSessionState(
            Id: Guid.NewGuid(),
            UserId: "user123",
            Status: InterviewStatus.Completed,
            TargetRole: "role",
            FocusArea: "area",
            Seniority: SeniorityLevel.Junior,
            InterviewType: InterviewType.Technical,
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            StartedAt: null, // Invalid for Completed status
            CompletedAt: createdAt.AddSeconds(10),
            QuestionCount: 5,
            AnsweredCount: 5,
            Feedback: null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InterviewSession.Restore(state));

        Assert.Contains("must have StartedAt", ex.Message);
    }
}

public sealed class InterviewTurn_StateHydration
{
    [Fact]
    public void ToState_ReturnCompleteState()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "topic"),
            createdAt: createdAt);

        var answeredAt = createdAt.AddSeconds(5);
        turn.RecordAnswer("My answer", answeredAt);
        turn.Evaluate(new AnswerEvaluation(OverallScore: new Score(80), Feedback: "Good!", Dimensions: []), createdAt.AddSeconds(10));

        var state = turn.ToState();

        Assert.Equal(turn.SessionId, state.SessionId);
        Assert.Equal(turn.UserId, state.UserId);
        Assert.Equal(turn.TurnNumber, state.TurnNumber);
        Assert.Equal(turn.Question, state.Question);
        Assert.Equal(turn.Answer, state.Answer);
        Assert.Equal(turn.Evaluation, state.Evaluation);
        Assert.Equal(turn.CreatedAt, state.CreatedAt);
        Assert.Equal(turn.UpdatedAt, state.UpdatedAt);
    }

    [Fact]
    public void Restore_WithValidState_RehydratesTurn()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var answeredAt = createdAt.AddSeconds(5);
        var sessionId = Guid.NewGuid();

        var state = new InterviewTurnState(
            SessionId: sessionId,
            UserId: "user123",
            TurnNumber: 2,
            Question: new InterviewQuestion("Question?", "topic"),
            Answer: new InterviewAnswer(text: "Answer text", answeredAt: answeredAt),
            Evaluation: new AnswerEvaluation(OverallScore: new Score(75), Feedback: "Good", Dimensions: []),
            CreatedAt: createdAt,
            UpdatedAt: createdAt.AddSeconds(10));

        var turn = InterviewTurn.Restore(state);

        Assert.Equal(sessionId, turn.SessionId);
        Assert.Equal("user123", turn.UserId);
        Assert.Equal(2, turn.TurnNumber);
        Assert.Equal("Question?", turn.Question.Text);
        Assert.True(turn.IsAnswered);
        Assert.True(turn.IsEvaluated);
        Assert.Equal(75, turn.Evaluation!.OverallScore.Value);
    }

    [Fact]
    public void Restore_RoundTrip_PreservesState()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var turn1 = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "user123",
            turnNumber: 3,
            question: new InterviewQuestion("Hard question?", "advanced-topic"),
            createdAt: createdAt);

        turn1.RecordAnswer("Detailed answer", createdAt.AddSeconds(30));
        turn1.Evaluate(new AnswerEvaluation(OverallScore: new Score(92), Feedback: "Excellent!", Dimensions: []), createdAt.AddSeconds(45));

        var state = turn1.ToState();
        var turn2 = InterviewTurn.Restore(state);
        var state2 = turn2.ToState();

        Assert.Equal(state, state2);
    }

    [Fact]
    public void Restore_WithEmptySessionId_ThrowsArgumentException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var state = new InterviewTurnState(
            SessionId: Guid.Empty,
            UserId: "user123",
            TurnNumber: 1,
            Question: new InterviewQuestion("Question?", "topic"),
            Answer: null,
            Evaluation: null,
            CreatedAt: createdAt,
            UpdatedAt: createdAt);

        var ex = Assert.Throws<ArgumentException>(() =>
            InterviewTurn.Restore(state));

        Assert.Contains("Session ID cannot be empty", ex.Message);
    }

    [Fact]
    public void Restore_WithEvaluationButNoAnswer_ThrowsArgumentException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var state = new InterviewTurnState(
            SessionId: Guid.NewGuid(),
            UserId: "user123",
            TurnNumber: 1,
            Question: new InterviewQuestion("Question?", "topic"),
            Answer: null,
            Evaluation: new AnswerEvaluation(OverallScore: new Score(75), Feedback: "Feedback", Dimensions: []), // Invalid: no answer
            CreatedAt: createdAt,
            UpdatedAt: createdAt);

        var ex = Assert.Throws<ArgumentException>(() =>
            InterviewTurn.Restore(state));

        Assert.Contains("Cannot have evaluation without answer", ex.Message);
    }

    [Fact]
    public void Restore_UpdatedBeforeCreated_ThrowsArgumentException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var state = new InterviewTurnState(
            SessionId: Guid.NewGuid(),
            UserId: "user123",
            TurnNumber: 1,
            Question: new InterviewQuestion("Question?", "topic"),
            Answer: null,
            Evaluation: null,
            CreatedAt: createdAt,
            UpdatedAt: createdAt.AddSeconds(-1)); // Invalid: before created

        var ex = Assert.Throws<ArgumentException>(() =>
            InterviewTurn.Restore(state));

        Assert.Contains("cannot be before created", ex.Message);
    }
}
