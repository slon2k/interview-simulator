using System.Net;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class GenerateSummaryTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task GenerateSummary_WhenCompletedWithoutSummary_PersistsSummary()
    {
        var store = new GenerateSummaryStore();
        var interviewId = store.SeedCompletedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest($"/api/interviews/{interviewId}/summary", "github|100");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var persisted = Assert.IsType<InterviewSessionState>(store.LastUpdatedSessionState);
        Assert.NotNull(persisted.InterviewSummary);
        Assert.NotEmpty(persisted.InterviewSummary.Text);
        Assert.NotNull(persisted.SummaryMetadata);
    }

    [Fact]
    public async Task GenerateSummary_WhenSessionNotFound_ReturnsNotFound()
    {
        var store = new GenerateSummaryStore();
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest($"/api/interviews/{Guid.NewGuid()}/summary", "github|100");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(store.UpdateCalled);
    }

    [Fact]
    public async Task GenerateSummary_WhenSessionBelongsToAnotherUser_ReturnsNotFound()
    {
        var store = new GenerateSummaryStore();
        var interviewId = store.SeedCompletedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest($"/api/interviews/{interviewId}/summary", "github|200");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(store.UpdateCalled);
    }

    [Fact]
    public async Task GenerateSummary_WhenSessionNotCompleted_ReturnsConflict()
    {
        var store = new GenerateSummaryStore();
        var interviewId = store.SeedActiveSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest($"/api/interviews/{interviewId}/summary", "github|100");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(store.UpdateCalled);
    }

    [Fact]
    public async Task GenerateSummary_WhenSummaryAlreadyExists_ReturnsConflict()
    {
        var store = new GenerateSummaryStore();
        var interviewId = store.SeedCompletedSessionWithSummary("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest($"/api/interviews/{interviewId}/summary", "github|100");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(store.UpdateCalled);
    }

    [Fact]
    public async Task GenerateSummary_WhenSummarizerFails_ReturnsServerErrorAndDoesNotPersist()
    {
        var store = new GenerateSummaryStore { SummarizerShouldFail = true };
        var interviewId = store.SeedCompletedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest($"/api/interviews/{interviewId}/summary", "github|100");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(store.UpdateCalled);
    }

    [Fact]
    public async Task GenerateSummary_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var store = new GenerateSummaryStore();
        var interviewId = store.SeedCompletedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/interviews/{interviewId}/summary");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(store.UpdateCalled);
    }

    private HttpClient CreateClientWithStore(GenerateSummaryStore store)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IInterviewStore>(_ => store);
                services.AddScoped<ISessionSummarizer>(_ => store.GetSummarizer());
            });
        }).CreateClient();
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(string path, string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, userId);
        request.Headers.Add(TestAuthHandler.LoginHeaderName, "invited-user");
        return request;
    }

    private sealed class GenerateSummaryStore : IInterviewStore
    {
        private readonly Dictionary<Guid, InterviewSessionState> _sessions = new();
        private readonly Dictionary<Guid, List<InterviewTurn>> _turns = new();
        private FakeSummarizer? _summarizer;

        public bool UpdateCalled { get; private set; }

        public InterviewSessionState? LastUpdatedSessionState { get; private set; }

        public bool SummarizerShouldFail { get; set; }

        public ISessionSummarizer GetSummarizer()
        {
            _summarizer ??= new FakeSummarizer(SummarizerShouldFail);
            return _summarizer;
        }

        public Guid SeedActiveSession(string userId)
        {
            var session = CreateStartedSession(userId, out _);
            _sessions[session.Id] = session.ToState();
            _turns[session.Id] = [];
            return session.Id;
        }

        public Guid SeedCompletedSession(string userId)
        {
            var session = CreateStartedSession(userId, out var createdAt);
            var turn = CreateAnsweredTurn(session.Id, userId, createdAt);
            session.Complete(createdAt.AddMinutes(5));

            _sessions[session.Id] = session.ToState();
            _turns[session.Id] = [turn];
            return session.Id;
        }

        public Guid SeedCompletedSessionWithSummary(string userId)
        {
            var session = CreateStartedSession(userId, out var createdAt);
            var turn = CreateAnsweredTurn(session.Id, userId, createdAt);
            var completedAt = createdAt.AddMinutes(5);
            session.Complete(completedAt);
            session.RecordSummary(
                new InterviewSummary("Existing summary", completedAt),
                new AiCallMetadata("summary-v1", "AzureOpenAI", "gpt-4o-mini", 100, 50),
                completedAt);

            _sessions[session.Id] = session.ToState();
            _turns[session.Id] = [turn];
            return session.Id;
        }

        private static InterviewSession CreateStartedSession(string userId, out DateTimeOffset createdAt)
        {
            createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);

            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 1);

            session.Start(createdAt.AddMinutes(1));
            return session;
        }

        private static InterviewTurn CreateAnsweredTurn(Guid sessionId, string userId, DateTimeOffset createdAt)
        {
            var turn = InterviewTurn.Create(
                sessionId: sessionId,
                userId: userId,
                turnNumber: 1,
                question: new InterviewQuestion("What is async/await?", "async"),
                questionGenerationMetadata: new AiCallMetadata("v1", "provider", "model", 10, 20),
                createdAt: createdAt.AddMinutes(1));

            var answeredAt = createdAt.AddMinutes(2);
            turn.RecordAnswer("Async/await is a pattern for asynchronous code", answeredAt);
            turn.RecordEvaluation(
                new AnswerEvaluation(
                    new Score(85),
                    new Feedback("Good answer"),
                    [new EvaluationDimension("quality", "Quality", new Score(85), new Feedback("Good answer"))]),
                metadata: null,
                updatedAt: answeredAt.AddSeconds(1));

            return turn;
        }

        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
            string userId,
            IReadOnlyList<InterviewStatus>? statuses,
            int limit,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>(
                _sessions.Values
                    .Where(state => state.UserId == userId)
                    .Where(state => statuses is null || statuses.Count == 0 || statuses.Contains(state.Status))
                    .Take(limit)
                    .Select(InterviewSession.Restore)
                    .ToArray());

        public Task<InterviewSession?> GetSessionAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            if (!_sessions.TryGetValue(sessionId, out var state) || !string.Equals(state.UserId, userId, StringComparison.Ordinal))
            {
                return Task.FromResult<InterviewSession?>(null);
            }

            return Task.FromResult<InterviewSession?>(InterviewSession.Restore(state));
        }

        public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
        {
            if (_turns.TryGetValue(sessionId, out var turns))
            {
                return Task.FromResult(turns.FirstOrDefault(turn => turn.TurnNumber == turnNumber));
            }

            return Task.FromResult<InterviewTurn?>(null);
        }

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            if (_turns.TryGetValue(sessionId, out var turns))
            {
                return Task.FromResult<IReadOnlyList<InterviewTurn>>(turns.AsReadOnly());
            }

            return Task.FromResult<IReadOnlyList<InterviewTurn>>(Array.Empty<InterviewTurn>());
        }

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            return Task.CompletedTask;
        }

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            _turns[session.Id] = [firstTurn];
            return Task.CompletedTask;
        }

        public Task SaveAnswerAsync(
            InterviewSession session,
            InterviewTurn answeredTurn,
            InterviewTurn? nextTurn = null,
            CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            return Task.CompletedTask;
        }

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            LastUpdatedSessionState = session.ToState();
            _sessions[session.Id] = LastUpdatedSessionState;
            return Task.CompletedTask;
        }

        private sealed class FakeSummarizer(bool shouldFail) : ISessionSummarizer
        {
            public Task<SessionSummaryResult> GenerateSummaryAsync(
                SessionSummaryRequest request,
                CancellationToken cancellationToken = default)
            {
                if (shouldFail)
                {
                    throw new InvalidOperationException("Summarizer configured to fail");
                }

                return Task.FromResult(new SessionSummaryResult(
                    Summary: "This candidate demonstrated good understanding of core concepts.",
                    AiMetadata: new AiCallMetadata("summary-v1", "AzureOpenAI", "gpt-4o-mini", 150, 80)));
            }
        }
    }
}
