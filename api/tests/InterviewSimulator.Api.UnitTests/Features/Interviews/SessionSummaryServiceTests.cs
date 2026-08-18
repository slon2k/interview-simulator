using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.Features.Common;

using Xunit;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews;

public sealed class SessionSummaryService_CreateSummaryAsync
{
    [Fact]
    public async Task CreateSummaryAsync_WithValidSessionAndTurns_GeneratesSummaryAndPersists()
    {
        // Arrange
        var userId = "github|123456";
        var createdAt = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero));

        var session = InterviewSession.Create(
            userId: userId,
            targetRole: "Backend Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 2);
        var sessionId = session.Id;

        var turn1 = CreateInterviewTurnWithAnswerAndEvaluation(
            turnNumber: 1,
            question: "What is async/await?",
            answer: "Async/await is a pattern for writing asynchronous code",
            overallScore: 85,
            createdAt: createdAt.AddMinutes(1));

        var turn2 = CreateInterviewTurnWithAnswerAndEvaluation(
            turnNumber: 2,
            question: "Explain dependency injection",
            answer: "Dependency injection is a design pattern",
            overallScore: 90,
            createdAt: createdAt.AddMinutes(5));

        var turns = new List<InterviewTurn> { turn1, turn2 };

        // Transition session to Completed state so summary can be recorded
        session.Start(createdAt.AddSeconds(1));
        session.Complete(createdAt.AddMinutes(10));

        var store = new FakeInterviewStore(session);
        store.SetTurns(turns);

        var summarizer = new FakeSessionSummarizer(
            new SessionSummaryResult(
                Summary: "Strong technical interview with good understanding of async patterns.",
                AiMetadata: new AiCallMetadata("summary-v1", "AzureOpenAI", "gpt-4o-mini", 100, 50)));

        var service = new SessionSummaryService(store, summarizer, timeProvider);

        // Act
        var result = await service.CreateSummaryAsync(sessionId, userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result.InterviewSummary);
        Assert.Equal("Strong technical interview with good understanding of async patterns.", result.InterviewSummary.Text);
        Assert.Equal(timeProvider.GetUtcNow(), result.InterviewSummary.CreatedAt);
        Assert.NotNull(result.SummaryMetadata);
        Assert.Equal("summary-v1", result.SummaryMetadata.PromptVersion);
        Assert.True(store.UpdateAsyncCalled);
    }

    [Fact]
    public async Task CreateSummaryAsync_WhenSessionNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "github|123456";
        var sessionId = Guid.NewGuid();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var store = new FakeInterviewStore(session: null);
        var summarizer = new FakeSessionSummarizer(new SessionSummaryResult("Summary", new AiCallMetadata("v1", "provider", "model", 10, 20)));
        var service = new SessionSummaryService(store, summarizer, timeProvider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateSummaryAsync(sessionId, userId, CancellationToken.None));

        Assert.Contains($"Session {sessionId} not found", exception.Message);
    }

    [Fact]
    public async Task CreateSummaryAsync_WhenSummarizerFails_PropagatesExceptionAndDoesNotPersist()
    {
        // Arrange
        var userId = "github|123456";
        var createdAt = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero));

        var session = InterviewSession.Create(
            userId: userId,
            targetRole: "Backend Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);
        var sessionId = session.Id;

        var turn = CreateInterviewTurnWithAnswerAndEvaluation(1, "Question?", "Answer", 80, createdAt.AddMinutes(1));
        var turns = new List<InterviewTurn> { turn };

        // Transition session to Completed state
        session.Start(createdAt.AddSeconds(1));
        session.Complete(createdAt.AddMinutes(5));

        var store = new FakeInterviewStore(session);
        store.SetTurns(turns);

        var summarizer = new FakeSessionSummarizer(null);
        var service = new SessionSummaryService(store, summarizer, timeProvider);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateSummaryAsync(sessionId, userId, CancellationToken.None));

        Assert.Null(session.InterviewSummary);
        Assert.False(store.UpdateAsyncCalled);
    }

    [Fact]
    public async Task CreateSummaryAsync_WhenTurnsHaveNoAnswerOrEvaluation_ExcludesIncompleteFromSummary()
    {
        // Arrange
        var userId = "github|123456";
        var createdAt = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero));

        var session = InterviewSession.Create(
            userId: userId,
            targetRole: "Backend Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 3);
        var sessionId = session.Id;

        // Complete turn (has both Answer and Evaluation)
        var completeTurn = CreateInterviewTurnWithAnswerAndEvaluation(
            turnNumber: 1,
            question: "Q1",
            answer: "A1",
            overallScore: 80,
            createdAt: createdAt.AddMinutes(1));

        // Incomplete turn (has Answer but no Evaluation)
        var incompleteAnswerOnly = CreateInterviewTurn(
            turnNumber: 2,
            question: "Q2",
            answer: new InterviewAnswer("A2", createdAt.AddMinutes(3)),
            evaluation: null,
            createdAt: createdAt.AddMinutes(2));

        // Incomplete turn (has no Answer or Evaluation)
        var incompleteNoAnswer = CreateInterviewTurn(
            turnNumber: 3,
            question: "Q3",
            answer: null,
            evaluation: null,
            createdAt: createdAt.AddMinutes(3));

        var turns = new List<InterviewTurn> { completeTurn, incompleteAnswerOnly, incompleteNoAnswer };
        
        // Transition session to Completed state
        session.Start(createdAt.AddSeconds(1));
        session.Complete(createdAt.AddMinutes(10));
        
        var store = new FakeInterviewStore(session);
        store.SetTurns(turns);

        var captureRequest = new SessionSummaryRequest?[1];
        var summaryResult = new SessionSummaryResult(
            Summary: "Only complete turn included",
            AiMetadata: new AiCallMetadata("v1", "AzureOpenAI", "gpt-4o", 100, 50));

        var summarizer = new FakeSessionSummarizer(summaryResult);
        summarizer.OnGenerateSummary = request => captureRequest[0] = request;

        var service = new SessionSummaryService(store, summarizer, timeProvider);

        // Act
        await service.CreateSummaryAsync(sessionId, userId, CancellationToken.None);

        // Assert - only 1 turn should be included in the request
        Assert.NotNull(captureRequest[0]);
        Assert.Single(captureRequest[0]!.Turns);
        Assert.Equal(1, captureRequest[0]!.Turns[0].TurnNumber);
        Assert.Equal("Q1", captureRequest[0]!.Turns[0].QuestionText);
    }

    [Fact]
    public async Task CreateSummaryAsync_WithEmptyTurnsAfterFiltering_GeneratesSummaryWithEmptyTurns()
    {
        // Arrange
        var userId = "github|123456";
        var createdAt = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero));

        var session = InterviewSession.Create(
            userId: userId,
            targetRole: "Backend Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);
        var sessionId = session.Id;

        // All turns are incomplete
        var incompleteOnlyTurns = new List<InterviewTurn>
        {
            CreateInterviewTurn(1, "Q1", answer: null, evaluation: null, createdAt.AddMinutes(1))
        };

        // Transition session to Completed state
        session.Start(createdAt.AddSeconds(1));
        session.Complete(createdAt.AddMinutes(5));
        
        var store = new FakeInterviewStore(session);
        store.SetTurns(incompleteOnlyTurns);

        var captureRequest = new SessionSummaryRequest?[1];
        var summaryResult = new SessionSummaryResult(
            Summary: "No complete turns to evaluate",
            AiMetadata: new AiCallMetadata("v1", "AzureOpenAI", "gpt-4o", 10, 5));

        var summarizer = new FakeSessionSummarizer(summaryResult);
        summarizer.OnGenerateSummary = request => captureRequest[0] = request;

        var service = new SessionSummaryService(store, summarizer, timeProvider);

        // Act
        var result = await service.CreateSummaryAsync(sessionId, userId, CancellationToken.None);

        // Assert - summary should be generated even with empty turns list
        Assert.NotNull(result.InterviewSummary);
        Assert.NotNull(captureRequest[0]);
        Assert.Empty(captureRequest[0]!.Turns);
    }

    [Fact]
    public async Task CreateSummaryAsync_TurnsAreOrderedByTurnNumber()
    {
        // Arrange
        var userId = "github|123456";
        var createdAt = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero));

        var session = InterviewSession.Create(
            userId: userId,
            targetRole: "Backend Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 3);
        var sessionId = session.Id;

        // Add turns in reverse order
        var turn3 = CreateInterviewTurnWithAnswerAndEvaluation(3, "Q3", "A3", 75, createdAt.AddMinutes(5));
        var turn1 = CreateInterviewTurnWithAnswerAndEvaluation(1, "Q1", "A1", 85, createdAt.AddMinutes(1));
        var turn2 = CreateInterviewTurnWithAnswerAndEvaluation(2, "Q2", "A2", 80, createdAt.AddMinutes(3));

        var turns = new List<InterviewTurn> { turn3, turn1, turn2 };
        
        // Transition session to Completed state
        session.Start(createdAt.AddSeconds(1));
        session.Complete(createdAt.AddMinutes(10));
        
        var store = new FakeInterviewStore(session);
        store.SetTurns(turns);

        var captureRequest = new SessionSummaryRequest?[1];
        var summaryResult = new SessionSummaryResult(
            Summary: "Ordered summary",
            AiMetadata: new AiCallMetadata("v1", "AzureOpenAI", "gpt-4o", 100, 50));

        var summarizer = new FakeSessionSummarizer(summaryResult);
        summarizer.OnGenerateSummary = request => captureRequest[0] = request;

        var service = new SessionSummaryService(store, summarizer, timeProvider);

        // Act
        await service.CreateSummaryAsync(sessionId, userId, CancellationToken.None);

        // Assert - turns should be ordered 1, 2, 3 despite input order
        Assert.NotNull(captureRequest[0]);
        Assert.Equal(3, captureRequest[0]!.Turns.Count);
        Assert.Equal(1, captureRequest[0]!.Turns[0].TurnNumber);
        Assert.Equal(2, captureRequest[0]!.Turns[1].TurnNumber);
        Assert.Equal(3, captureRequest[0]!.Turns[2].TurnNumber);
    }

    [Fact]
    public async Task CreateSummaryAsync_MapsAllTurnPropertiesCorrectly()
    {
        // Arrange
        var userId = "github|123456";
        var createdAt = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero));

        var session = InterviewSession.Create(
            userId: userId,
            targetRole: "Backend Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 1);
        var sessionId = session.Id;

        var dimension1 = new EvaluationDimension("clarity", "Clarity", new Score(85), new Feedback("Clear explanation"));
        var dimension2 = new EvaluationDimension("depth", "Depth", new Score(90), new Feedback("Good depth"));

        var evaluation = new AnswerEvaluation(
            new Score(85),
            new Feedback("Good answers overall"),
            new[] { dimension1, dimension2 });

        var turn = CreateInterviewTurn(
            turnNumber: 1,
            question: new InterviewQuestion("Explain caching", "caching"),
            answer: new InterviewAnswer("Caching stores frequently accessed data", createdAt.AddSeconds(30)),
            evaluation: evaluation,
            createdAt: createdAt);

        var turns = new List<InterviewTurn> { turn };
        
        // Transition session to Completed state
        session.Start(createdAt.AddSeconds(1));
        session.Complete(createdAt.AddMinutes(5));
        
        var store = new FakeInterviewStore(session);
        store.SetTurns(turns);

        var captureRequest = new SessionSummaryRequest?[1];
        var summaryResult = new SessionSummaryResult(
            Summary: "Mapped summary",
            AiMetadata: new AiCallMetadata("v1", "AzureOpenAI", "gpt-4o", 100, 50));

        var summarizer = new FakeSessionSummarizer(summaryResult);
        summarizer.OnGenerateSummary = request => captureRequest[0] = request;

        var service = new SessionSummaryService(store, summarizer, timeProvider);

        // Act
        await service.CreateSummaryAsync(sessionId, userId, CancellationToken.None);

        // Assert - all properties should be mapped
        Assert.NotNull(captureRequest[0]);
        Assert.Single(captureRequest[0]!.Turns);

        var mappedTurn = captureRequest[0]!.Turns[0];
        Assert.Equal(1, mappedTurn.TurnNumber);
        Assert.Equal("Explain caching", mappedTurn.QuestionText);
        Assert.Equal("caching", mappedTurn.QuestionTopic);
        Assert.Equal("Caching stores frequently accessed data", mappedTurn.AnswerText);
        Assert.Equal(85, mappedTurn.OverallScore);
        Assert.Equal("Good answers overall", mappedTurn.Feedback);
        Assert.Equal(2, mappedTurn.Dimensions.Count);
    }

    // Helpers

    private static InterviewTurn CreateInterviewTurnWithAnswerAndEvaluation(
        int turnNumber,
        string question,
        string answer,
        int overallScore,
        DateTimeOffset createdAt)
    {
        var evaluation = new AnswerEvaluation(
            new Score(overallScore),
            new Feedback("Good answer"),
            [new EvaluationDimension("quality", "Quality", new Score(overallScore), new Feedback("Good answer"))]);

        return CreateInterviewTurn(
            turnNumber: turnNumber,
            question: new InterviewQuestion(question, "topic"),
            answer: new InterviewAnswer(answer, createdAt.AddSeconds(30)),
            evaluation: evaluation,
            createdAt: createdAt);
    }

    private static InterviewTurn CreateInterviewTurn(
        int turnNumber,
        string question,
        InterviewAnswer? answer = null,
        AnswerEvaluation? evaluation = null,
        DateTimeOffset? createdAt = null)
    {
        var now = createdAt ?? DateTimeOffset.UtcNow;
        var q = new InterviewQuestion(question, "topic");
        return CreateInterviewTurn(turnNumber, q, answer, evaluation, now);
    }

    private static InterviewTurn CreateInterviewTurn(
        int turnNumber,
        InterviewQuestion question,
        InterviewAnswer? answer,
        AnswerEvaluation? evaluation,
        DateTimeOffset createdAt)
    {
        var turn = InterviewTurn.Create(
            sessionId: Guid.NewGuid(),
            userId: "github|test",
            turnNumber: turnNumber,
            question: question,
            questionGenerationMetadata: new AiCallMetadata("v1", "provider", "model", 10, 20),
            createdAt: createdAt);

        // Manually set Answer and Evaluation since Create only initializes Question
        if (answer != null)
        {
            turn.RecordAnswer(answer.Text, answer.AnsweredAt);
        }

        if (evaluation != null)
        {
            // Evaluation must be recorded after the answer is recorded
            var answeredAt = answer?.AnsweredAt ?? createdAt.AddSeconds(30);
            turn.RecordEvaluation(evaluation, metadata: null, updatedAt: answeredAt.AddSeconds(1));
        }

        return turn;
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private sealed class FakeInterviewStore : IInterviewStore
    {
        private readonly InterviewSession? _session;
        private readonly Dictionary<Guid, InterviewSessionState> _sessions = new();
        private List<InterviewTurn> _turns = new();

        public bool UpdateAsyncCalled { get; private set; }

        public FakeInterviewStore(InterviewSession? session)
        {
            _session = session;
            if (session != null)
            {
                _sessions[session.Id] = session.ToState();
            }
        }

        public void SetTurns(IEnumerable<InterviewTurn> turns)
        {
            _turns = new List<InterviewTurn>(turns);
        }

        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
            string userId,
            IReadOnlyList<InterviewStatus>? statuses,
            int limit,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>(Array.Empty<InterviewSession>());

        public Task<InterviewSession?> GetSessionAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            if (_session == null || _session.UserId != userId || _session.Id != sessionId)
            {
                return Task.FromResult<InterviewSession?>(null);
            }

            return Task.FromResult<InterviewSession?>(_session);
        }

        public Task<InterviewTurn?> GetTurnAsync(
            string userId,
            Guid sessionId,
            int turnNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult<InterviewTurn?>(null);

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>(_turns.AsReadOnly());

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveAnswerAsync(
            InterviewSession session,
            InterviewTurn answeredTurn,
            InterviewTurn? nextTurn = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            UpdateAsyncCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSessionSummarizer : ISessionSummarizer
    {
        private readonly SessionSummaryResult _result;
        private readonly bool _shouldFail;

        public Action<SessionSummaryRequest>? OnGenerateSummary { get; set; }

        public FakeSessionSummarizer(SessionSummaryResult? result)
        {
            _shouldFail = result is null;
            _result = result ?? new SessionSummaryResult(
                "Fallback summary",
                new AiCallMetadata("v1", "provider", "model", 10, 10));
        }

        public Task<SessionSummaryResult> GenerateSummaryAsync(
            SessionSummaryRequest request,
            CancellationToken cancellationToken = default)
        {
            OnGenerateSummary?.Invoke(request);
            if (_shouldFail)
            {
                throw new InvalidOperationException("Summarizer configured to fail");
            }
            return Task.FromResult(_result);
        }
    }
}
