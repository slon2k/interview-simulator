using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Common;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews;

public sealed class InterviewSession_Create
{
    [Fact]
    public void Create_WithValidArguments_CreatesSessionInCreatedStatus()
    {
        var userId = "github|123456";
        var targetRole = "backend-engineer";
        var focusArea = "dotnet-async";
        var seniority = SeniorityLevel.Middle;
        var interviewType = InterviewType.Technical;
        var createdAt = DateTimeOffset.UtcNow;
        var questionCount = 5;

        var session = InterviewSession.Create(
            userId: userId,
            targetRole: targetRole,
            focusArea: focusArea,
            seniority: seniority,
            interviewType: interviewType,
            createdAt: createdAt,
            questionCount: questionCount);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(targetRole, session.TargetRole);
        Assert.Equal(focusArea, session.FocusArea);
        Assert.Equal(seniority, session.Seniority);
        Assert.Equal(interviewType, session.InterviewType);
        Assert.Equal(InterviewStatus.Created, session.Status);
        Assert.Equal(createdAt, session.CreatedAt);
        Assert.Equal(createdAt, session.UpdatedAt);
        Assert.Null(session.StartedAt);
        Assert.Null(session.CompletedAt);
        Assert.Equal(questionCount, session.QuestionCount);
        Assert.Equal(0, session.AnsweredCount);
        Assert.Null(session.SessionResult);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            InterviewSession.Create(
                userId: userId!,
                targetRole: "role",
                focusArea: "area",
                seniority: SeniorityLevel.Junior,
                interviewType: InterviewType.Technical,
                createdAt: DateTimeOffset.UtcNow,
                questionCount: 5));

        Assert.Equal("userId", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTargetRole_ThrowsArgumentException(string? targetRole)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            InterviewSession.Create(
                userId: "user123",
                targetRole: targetRole!,
                focusArea: "area",
                seniority: SeniorityLevel.Junior,
                interviewType: InterviewType.Technical,
                createdAt: DateTimeOffset.UtcNow,
                questionCount: 5));

        Assert.Equal("targetRole", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidFocusArea_ThrowsArgumentException(string? focusArea)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            InterviewSession.Create(
                userId: "user123",
                targetRole: "role",
                focusArea: focusArea!,
                seniority: SeniorityLevel.Junior,
                interviewType: InterviewType.Technical,
                createdAt: DateTimeOffset.UtcNow,
                questionCount: 5));

        Assert.Equal("focusArea", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidQuestionCount_ThrowsArgumentOutOfRangeException(int questionCount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            InterviewSession.Create(
                userId: "user123",
                targetRole: "role",
                focusArea: "area",
                seniority: SeniorityLevel.Junior,
                interviewType: InterviewType.Technical,
                createdAt: DateTimeOffset.UtcNow,
                questionCount: questionCount));

        Assert.Equal("questionCount", ex.ParamName);
    }
}

public sealed class InterviewSession_Start
{
    [Fact]
    public void Start_FromCreatedStatus_TransitionsToActive()
    {
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: DateTimeOffset.UtcNow,
            questionCount: 5);

        var startedAt = session.CreatedAt.AddSeconds(10);
        session.Start(startedAt);

        Assert.Equal(InterviewStatus.Active, session.Status);
        Assert.Equal(startedAt, session.StartedAt);
        Assert.Equal(startedAt, session.UpdatedAt);
    }

    [Fact]
    public void Start_FromActiveStatus_ThrowsDomainConflictException()
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

        var ex = Assert.Throws<DomainConflictException>(() =>
            session.Start(createdAt.AddSeconds(2)));

        Assert.Equal(InterviewSession.Errors.SessionNotCreated.Code, ex.Code);
    }

    [Fact]
    public void Start_WithTimestampBeforeCreated_ThrowsInvalidOperationException()
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

        var ex = Assert.Throws<InvalidOperationException>(() =>
            session.Start(createdAt.AddSeconds(-1)));

        Assert.Contains("cannot be before created", ex.Message);
    }
}

public sealed class InterviewSession_RecordAnswer
{
    [Fact]
    public void RecordAnswer_WhenNotComplete_IncrementsAnsweredCount()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 3);

        session.Start(createdAt.AddSeconds(1));

        var result = new SessionResult(new Score(90));
        var isComplete = session.RecordAnswer(result, createdAt.AddSeconds(2));

        Assert.False(isComplete);
        Assert.Equal(1, session.AnsweredCount);
        Assert.Equal(InterviewStatus.Active, session.Status);
    }

    [Fact]
    public void RecordAnswer_WhenLastAnswer_CompletesSession()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        session.Start(createdAt.AddSeconds(1));

        var completedAt = createdAt.AddSeconds(2);
        var result = new SessionResult(new Score(100));
        var isComplete = session.RecordAnswer(result, completedAt);

        Assert.True(isComplete);
        Assert.Equal(1, session.AnsweredCount);
        Assert.Equal(InterviewStatus.Completed, session.Status);
        Assert.Equal(completedAt, session.CompletedAt);
    }

    [Fact]
    public void RecordAnswer_WhenNotActive_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        var ex = Assert.Throws<DomainConflictException>(() =>
            session.RecordAnswer(new SessionResult(new Score(90)), createdAt.AddSeconds(1)));

        Assert.Equal(InterviewSession.Errors.SessionNotActive.Code, ex.Code);
    }

    [Fact]
    public void RecordAnswer_BeyondQuestionCount_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        session.Start(createdAt.AddSeconds(1));
        var result = new SessionResult(new Score(90));
        session.RecordAnswer(result, createdAt.AddSeconds(2));

        // Session auto-completes when all answers are recorded,
        // so it's no longer active and throws "not active" error
        var ex = Assert.Throws<DomainConflictException>(() =>
            session.RecordAnswer(new SessionResult(new Score(90)), createdAt.AddSeconds(3)));

        // After last answer, session auto-completes and is no longer active
        Assert.Equal(InterviewStatus.Completed, session.Status);
        Assert.Equal(InterviewSession.Errors.SessionNotActive.Code, ex.Code);
    }

    [Fact]
    public void RecordAnswer_BeforeStarted_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        session.Start(createdAt.AddSeconds(1));

        var ex = Assert.Throws<DomainConflictException>(() =>
            session.RecordAnswer(new SessionResult(new Score(90)), createdAt));

        Assert.Equal(InterviewSession.Errors.AnsweredBeforeStartedAt.Code, ex.Code);
    }
}

public sealed class InterviewSession_Complete
{
    [Fact]
    public void Complete_FromActiveStatus_TransitionsToCompleted()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        session.Start(createdAt.AddSeconds(1));

        var completedAt = createdAt.AddSeconds(10);
        session.Complete(completedAt);

        Assert.Equal(InterviewStatus.Completed, session.Status);
        Assert.Equal(completedAt, session.CompletedAt);
        Assert.Equal(completedAt, session.UpdatedAt);
    }

    [Fact]
    public void Complete_FromCreatedStatus_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        var ex = Assert.Throws<DomainConflictException>(() =>
            session.Complete(createdAt.AddSeconds(1)));

        Assert.Equal(InterviewSession.Errors.SessionNotActive.Code, ex.Code);
    }

    [Fact]
    public void Complete_WithTimestampBeforeStarted_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var startedAt = createdAt.AddSeconds(1);
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        session.Start(startedAt);

        var ex = Assert.Throws<DomainConflictException>(() =>
            session.Complete(startedAt.AddSeconds(-1)));

        Assert.Equal(InterviewSession.Errors.CompletedBeforeStartedAt.Code, ex.Code);
    }
}

public sealed class InterviewSession_Evaluate
{
    [Fact]
    public void Evaluate_OnCompletedSession_RecordsFeedback()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        session.Start(createdAt.AddSeconds(1));
        session.RecordAnswer(new SessionResult(new Score(90)), createdAt.AddSeconds(2));

        var feedback = new SessionResult(new Score(85));
        var evaluatedAt = createdAt.AddSeconds(3);
        session.RecordResult(feedback, evaluatedAt);

        Assert.NotNull(session.SessionResult);
        Assert.Equal(85, session.SessionResult.OverallScore);
        Assert.Equal(evaluatedAt, session.UpdatedAt);
    }

    [Fact]
    public void Evaluate_OnActiveSession_ThrowsDomainConflictException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        session.Start(createdAt.AddSeconds(1));

        var feedback = new SessionResult(new Score(85));
        var ex = Assert.Throws<DomainConflictException>(() =>
            session.RecordResult(feedback, createdAt.AddSeconds(2)));

        Assert.Equal(InterviewSession.Errors.SessionNotCompleted.Code, ex.Code);
    }

    [Fact]
    public void Evaluate_WithTimestampBeforeCompleted_ThrowsInvalidOperationException()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);

        session.Start(createdAt.AddSeconds(1));
        var completedAt = createdAt.AddSeconds(2);
        session.RecordAnswer(new SessionResult(new Score(90)), completedAt);
        session.Complete(completedAt);

        var feedback = new SessionResult(new Score(85));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            session.RecordResult(feedback, completedAt.AddSeconds(-1)));

        Assert.Contains("cannot be before completed", ex.Message);
    }
}
