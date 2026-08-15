using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.Infrastructure.Interviews;

namespace InterviewSimulator.Api.UnitTests.Infrastructure.Interviews;

public sealed class CosmosSessionDocument_Mapping
{
    [Fact]
    public void FromDomain_WithConcurrencyToken_MapsEtag()
    {
        var session = InterviewSession.Restore(new InterviewSessionState(
            Id: Guid.NewGuid(),
            UserId: "user123",
            Status: InterviewStatus.Created,
            TargetRole: "Software Engineer",
            FocusArea: "Backend",
            Seniority: SeniorityLevel.Middle,
            InterviewType: InterviewType.Technical,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            StartedAt: null,
            CompletedAt: null,
            QuestionCount: 5,
            AnsweredCount: 0,
            SessionResult: null,
            InterviewSummary: null,
            SummaryMetadata: null,
            ConcurrencyToken: "etag-session-1"));

        var doc = CosmosSessionDocument.FromDomain(session);

        Assert.Equal("etag-session-1", doc.ETag);
    }

    [Fact]
    public void ToDomain_WithEtag_MapsConcurrencyToken()
    {
        var now = DateTimeOffset.UtcNow;
        var doc = new CosmosSessionDocument
        {
            Id = $"session|{Guid.NewGuid()}",
            UserId = "user123",
            SessionId = Guid.NewGuid().ToString(),
            Status = "Created",
            TargetRole = "Software Engineer",
            FocusArea = "Backend",
            Seniority = "Middle",
            InterviewType = "Technical",
            CreatedAt = now,
            UpdatedAt = now,
            QuestionCount = 5,
            AnsweredCount = 0,
            ETag = "etag-session-2"
        };

        var session = doc.ToDomain();

        Assert.Equal("etag-session-2", session.ConcurrencyToken);
    }

    [Fact]
    public void FromDomain_WithCompleteSession_MapsAllFields()
    {
        var sessionId = Guid.NewGuid();
        var userId = "user123";
        var createdAt = DateTimeOffset.UtcNow;
        var startedAt = createdAt.AddSeconds(10);
        var completedAt = createdAt.AddSeconds(100);

        var session = InterviewSession.Create(
            userId: userId,
            targetRole: "Software Engineer",
            focusArea: "Backend",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 5);

        // Manually set ID for testing
        var sessionWithId = InterviewSession.Restore(new InterviewSessionState(
            Id: sessionId,
            UserId: userId,
            Status: InterviewStatus.Completed,
            TargetRole: "Software Engineer",
            FocusArea: "Backend",
            Seniority: SeniorityLevel.Middle,
            InterviewType: InterviewType.Technical,
            CreatedAt: createdAt,
            UpdatedAt: completedAt,
            StartedAt: startedAt,
            CompletedAt: completedAt,
            QuestionCount: 5,
            AnsweredCount: 5,
            SessionResult: new SessionResult(new Score(85)),
            InterviewSummary: null,
            SummaryMetadata: null));

        var doc = CosmosSessionDocument.FromDomain(sessionWithId);

        Assert.Equal($"session|{sessionId}", doc.Id);
        Assert.Equal(userId, doc.UserId);
        Assert.Equal(sessionId.ToString(), doc.SessionId);
        Assert.Equal("session", doc.Type);
        Assert.Equal(1, doc.SchemaVersion);
        Assert.Equal("Software Engineer", doc.TargetRole);
        Assert.Equal("Backend", doc.FocusArea);
        Assert.Equal("Middle", doc.Seniority);
        Assert.Equal("Technical", doc.InterviewType);
        Assert.Equal(createdAt, doc.CreatedAt);
        Assert.Equal(completedAt, doc.UpdatedAt);
        Assert.Equal(startedAt, doc.StartedAt);
        Assert.Equal(completedAt, doc.CompletedAt);
        Assert.Equal(5, doc.QuestionCount);
        Assert.Equal(5, doc.AnsweredCount);
        Assert.NotNull(doc.Result);
        Assert.Equal(85, doc.Result.TotalScore);
    }

    [Fact]
    public void ToDomain_WithCompleteDocument_RestoresAllFields()
    {
        var sessionId = Guid.NewGuid();
        var userId = "user123";
        var createdAt = DateTimeOffset.UtcNow;
        var startedAt = createdAt.AddSeconds(10);
        var completedAt = createdAt.AddSeconds(100);

        var doc = new CosmosSessionDocument
        {
            Id = $"session|{sessionId}",
            UserId = userId,
            SessionId = sessionId.ToString(),
            Type = "session",
            SchemaVersion = 1,
            Status = "Completed",
            TargetRole = "Software Engineer",
            FocusArea = "Backend",
            Seniority = "Middle",
            InterviewType = "Technical",
            CreatedAt = createdAt,
            UpdatedAt = completedAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            QuestionCount = 5,
            AnsweredCount = 5,
            Result = new CosmosSessionResultDocument
            {
                TotalScore = 85
            }
        };

        var session = doc.ToDomain();

        Assert.Equal(sessionId, session.Id);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(InterviewStatus.Completed, session.Status);
        Assert.Equal("Software Engineer", session.TargetRole);
        Assert.Equal("Backend", session.FocusArea);
        Assert.Equal(SeniorityLevel.Middle, session.Seniority);
        Assert.Equal(InterviewType.Technical, session.InterviewType);
        Assert.Equal(createdAt, session.CreatedAt);
        Assert.Equal(completedAt, session.CompletedAt);
        Assert.Equal(startedAt, session.StartedAt);
        Assert.Equal(completedAt, session.CompletedAt);
        Assert.Equal(5, session.QuestionCount);
        Assert.Equal(5, session.AnsweredCount);
        Assert.NotNull(session.SessionResult);
        Assert.Equal(85, session.SessionResult.OverallScore);
    }

    [Fact]
    public void SummaryAndMetadata_RoundTripThroughCosmosDocument()
    {
        var sessionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var summaryCreatedAt = createdAt.AddMinutes(5);
        var session = InterviewSession.Restore(new InterviewSessionState(
            Id: sessionId,
            UserId: "user123",
            Status: InterviewStatus.Completed,
            TargetRole: "Software Engineer",
            FocusArea: "Backend",
            Seniority: SeniorityLevel.Middle,
            InterviewType: InterviewType.Technical,
            CreatedAt: createdAt,
            UpdatedAt: summaryCreatedAt,
            StartedAt: createdAt.AddSeconds(1),
            CompletedAt: createdAt.AddMinutes(1),
            QuestionCount: 5,
            AnsweredCount: 5,
            SessionResult: new SessionResult(new Score(85)),
            InterviewSummary: new InterviewSummary("Strong implementation with clear tradeoffs.", summaryCreatedAt),
            SummaryMetadata: new AiCallMetadata(
                PromptVersion: "summary-v1",
                Provider: "AzureOpenAI",
                Model: "gpt-4o-mini",
                PromptTokens: 120,
                CompletionTokens: 80)));

        var document = CosmosSessionDocument.FromDomain(session);

        Assert.NotNull(document.Summary);
        Assert.Equal("Strong implementation with clear tradeoffs.", document.Summary.Text);
        Assert.Equal(summaryCreatedAt, document.Summary.CreatedAt);
        Assert.NotNull(document.SummaryMetadata);
        Assert.Equal("summary-v1", document.SummaryMetadata.PromptVersion);
        Assert.Equal("AzureOpenAI", document.SummaryMetadata.Provider);
        Assert.Equal("gpt-4o-mini", document.SummaryMetadata.Model);
        Assert.Equal(120, document.SummaryMetadata.PromptTokens);
        Assert.Equal(80, document.SummaryMetadata.CompletionTokens);

        var restoredSession = document.ToDomain();

        Assert.Equal(session.InterviewSummary, restoredSession.InterviewSummary);
        Assert.Equal(session.SummaryMetadata, restoredSession.SummaryMetadata);
    }

    [Fact]
    public void RoundTrip_Session_PreservesAllState()
    {
        var userId = "user123";
        var createdAt = DateTimeOffset.UtcNow;

        var originalSession = InterviewSession.Create(
            userId: userId,
            targetRole: "Senior Engineer",
            focusArea: "System Design",
            seniority: SeniorityLevel.Senior,
            interviewType: InterviewType.SystemDesign,
            createdAt: createdAt,
            questionCount: 3);

        originalSession.Start(createdAt.AddSeconds(5));

        originalSession.RecordAnswer(new SessionResult(new Score(90)), createdAt.AddSeconds(30));
        originalSession.RecordAnswer(new SessionResult(new Score(80)), createdAt.AddSeconds(60));
        originalSession.RecordAnswer(new SessionResult(new Score(85)), createdAt.AddSeconds(90));

        // Map to document and back
        var doc = CosmosSessionDocument.FromDomain(originalSession);
        var restoredSession = doc.ToDomain();

        // Verify all fields match
        Assert.Equal(originalSession.Id, restoredSession.Id);
        Assert.Equal(originalSession.UserId, restoredSession.UserId);
        Assert.Equal(originalSession.Status, restoredSession.Status);
        Assert.Equal(originalSession.TargetRole, restoredSession.TargetRole);
        Assert.Equal(originalSession.FocusArea, restoredSession.FocusArea);
        Assert.Equal(originalSession.Seniority, restoredSession.Seniority);
        Assert.Equal(originalSession.InterviewType, restoredSession.InterviewType);
        Assert.Equal(originalSession.QuestionCount, restoredSession.QuestionCount);
        Assert.Equal(originalSession.AnsweredCount, restoredSession.AnsweredCount);
        Assert.Equal(originalSession.SessionResult?.OverallScore, restoredSession.SessionResult?.OverallScore);
    }

    [Fact]
    public void FromDomain_WithNullFeedback_MapsCorrectly()
    {
        var session = InterviewSession.Create(
            userId: "user123",
            targetRole: "role",
            focusArea: "area",
            seniority: SeniorityLevel.Junior,
            interviewType: InterviewType.Technical,
            createdAt: DateTimeOffset.UtcNow,
            questionCount: 5);

        var doc = CosmosSessionDocument.FromDomain(session);

        Assert.Null(doc.Result);
    }

    [Fact]
    public void ToDomain_WithNullFeedback_MapsCorrectly()
    {
        var doc = new CosmosSessionDocument
        {
            Id = $"session|{Guid.NewGuid()}",
            UserId = "user123",
            SessionId = Guid.NewGuid().ToString(),
            Type = "session",
            SchemaVersion = 1,
            Status = "Created",
            TargetRole = "role",
            FocusArea = "area",
            Seniority = "Junior",
            InterviewType = "Technical",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            StartedAt = null,
            CompletedAt = null,
            QuestionCount = 5,
            AnsweredCount = 0,
            Result = null
        };

        var session = doc.ToDomain();

        Assert.Null(session.SessionResult);
    }

    [Theory]
    [InlineData("Created")]
    [InlineData("Active")]
    [InlineData("Completed")]
    public void Mapping_WithDifferentStatuses_MapsCorrectly(string status)
    {
        var createdAt = DateTimeOffset.UtcNow;
        var startedAt = createdAt.AddSeconds(10);
        var completedAt = createdAt.AddSeconds(100);

        var doc = new CosmosSessionDocument
        {
            Id = $"session|{Guid.NewGuid()}",
            UserId = "user123",
            SessionId = Guid.NewGuid().ToString(),
            Type = "session",
            SchemaVersion = 1,
            Status = status,
            TargetRole = "role",
            FocusArea = "area",
            Seniority = "Junior",
            InterviewType = "Technical",
            CreatedAt = createdAt,
            UpdatedAt = status == "Completed" ? completedAt : (status == "Active" ? startedAt : createdAt),
            StartedAt = status == "Created" ? null : startedAt,
            CompletedAt = status == "Completed" ? completedAt : null,
            QuestionCount = 5,
            AnsweredCount = status == "Completed" ? 5 : 0
        };

        var session = doc.ToDomain();

        var expectedStatus = Enum.Parse<InterviewStatus>(status);
        Assert.Equal(expectedStatus, session.Status);
    }

    [Theory]
    [InlineData("Technical")]
    [InlineData("Behavioral")]
    [InlineData("SystemDesign")]
    public void Mapping_WithDifferentInterviewTypes_MapsCorrectly(string interviewType)
    {
        var doc = new CosmosSessionDocument
        {
            Id = $"session|{Guid.NewGuid()}",
            UserId = "user123",
            SessionId = Guid.NewGuid().ToString(),
            Type = "session",
            SchemaVersion = 1,
            Status = "Created",
            TargetRole = "role",
            FocusArea = "area",
            Seniority = "Junior",
            InterviewType = interviewType,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            QuestionCount = 5,
            AnsweredCount = 0
        };

        var session = doc.ToDomain();

        var expectedType = Enum.Parse<InterviewType>(interviewType);
        Assert.Equal(expectedType, session.InterviewType);
    }
}
