using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

/// <summary>
/// Integration tests verifying SessionSummaryService is invoked and summary is persisted
/// during interview completion flow (both CompleteInterview endpoint and SubmitAnswer final answer).
/// </summary>
public sealed class CompleteInterviewSummaryPersistenceTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task CompleteInterview_WhenActive_PersistsSummaryInSession()
    {
        // Arrange
        var store = new SummaryPersistenceStore();
        var interviewId = store.SeedActiveSession("github|100");
        using var client = CreateClientWithStore(store);

        using var completeRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{interviewId}/complete",
            "github|100",
            "invited-user");

        // Act
        var completeResponse = await client.SendAsync(completeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);
        Assert.True(store.UpdateCalled);

        var persisted = Assert.IsType<InterviewSessionState>(store.LastUpdatedSessionState);
        Assert.Equal(InterviewStatus.Completed, persisted.Status);
        Assert.NotNull(persisted.CompletedAt);
        
        // Verify summary was recorded
        Assert.NotNull(persisted.InterviewSummary);
        Assert.NotEmpty(persisted.InterviewSummary.Text);
        Assert.NotNull(persisted.SummaryMetadata);
    }

    [Fact]
    public async Task SubmitAnswer_WhenFinalAnswer_GeneratesSummaryAndPersists()
    {
        // Arrange
        var store = new SummaryPersistenceStore();
        var interviewId = store.SeedActiveSessionWithOneTurn("github|100");
        using var client = CreateClientWithStore(store);

        using var answerRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{interviewId}/answers",
            "github|100",
            "invited-user");

        answerRequest.Content = JsonContent.Create(new
        {
            turnNumber = 1,
            answer = "This is the final answer to the last question",
            answerGenerationFocus = "technical depth"
        });

        // Act
        var answerResponse = await client.SendAsync(answerRequest);

        // Assert - should complete normally
        Assert.True(
            answerResponse.StatusCode == HttpStatusCode.OK ||
            answerResponse.StatusCode == HttpStatusCode.NoContent,
            $"Expected OK or NoContent, got {answerResponse.StatusCode}");

        // The session should have been persisted with summary after final answer
        var persisted = store.LastUpdatedSessionState;
        Assert.NotNull(persisted);
        Assert.Equal(InterviewStatus.Completed, persisted.Status);
        
        // Verify summary was recorded by second or later update
        if (store.AllUpdatedStates.Count >= 2)
        {
            var finalState = store.AllUpdatedStates[^1];
            Assert.NotNull(finalState.InterviewSummary);
            Assert.NotEmpty(finalState.InterviewSummary.Text);
        }
    }

    [Fact]
    public async Task CompleteInterview_WhenNoTurns_StillCompletesAndGeneratesSummary()
    {
        // Arrange - session with no turns (edge case)
        var store = new SummaryPersistenceStore();
        var interviewId = store.SeedActiveSession("github|100");
        using var client = CreateClientWithStore(store);

        using var completeRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{interviewId}/complete",
            "github|100",
            "invited-user");

        // Act
        var completeResponse = await client.SendAsync(completeRequest);

        // Assert - should still complete successfully even with empty turns
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);
        Assert.True(store.UpdateCalled);

        var persisted = Assert.IsType<InterviewSessionState>(store.LastUpdatedSessionState);
        Assert.Equal(InterviewStatus.Completed, persisted.Status);
        
        // Even with no turns, summarizer should still be called and record something
        Assert.NotNull(persisted.InterviewSummary);
    }

    [Fact]
    public async Task CompleteInterview_WhenSummarizerFails_StillReturnsSuccess()
    {
        // Arrange - store configured to make summarizer fail
        var store = new SummaryPersistenceStore { SummarizerShouldFail = true };
        var interviewId = store.SeedActiveSession("github|100");
        using var client = CreateClientWithStore(store);

        using var completeRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{interviewId}/complete",
            "github|100",
            "invited-user");

        // Act
        var completeResponse = await client.SendAsync(completeRequest);

        // Assert - endpoint should still succeed (graceful degradation)
        // Summary generation failure shouldn't fail the HTTP response
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

        var persisted = Assert.IsType<InterviewSessionState>(store.LastUpdatedSessionState);
        Assert.Equal(InterviewStatus.Completed, persisted.Status);
        
        // But summary won't be recorded since summarizer failed
        Assert.Null(persisted.InterviewSummary);
    }

    private HttpClient CreateClientWithStore(SummaryPersistenceStore store)
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

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string path,
        string userId,
        string login)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, userId);
        request.Headers.Add(TestAuthHandler.LoginHeaderName, login);
        return request;
    }

    private sealed class SummaryPersistenceStore : IInterviewStore
    {
        private readonly Dictionary<Guid, InterviewSessionState> _sessions = new();
        private readonly Dictionary<Guid, List<InterviewTurn>> _turns = new();

        public bool UpdateCalled { get; private set; }

        public InterviewSessionState? LastUpdatedSessionState { get; private set; }

        public List<InterviewSessionState> AllUpdatedStates { get; } = new();

        public bool SummarizerShouldFail { get; set; }

        private FakeSummarizer? _summarizer;

        public ISessionSummarizer GetSummarizer()
        {
            _summarizer ??= new FakeSummarizer(SummarizerShouldFail);
            return _summarizer;
        }

        public Guid SeedActiveSession(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var startedAt = createdAt.AddMinutes(1);

            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 3);

            session.Start(startedAt);

            _sessions[session.Id] = session.ToState();
            _turns[session.Id] = new List<InterviewTurn>();
            return session.Id;
        }

        public Guid SeedActiveSessionWithOneTurn(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var startedAt = createdAt.AddMinutes(1);

            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 1);

            session.Start(startedAt);

            var question = new InterviewQuestion("What is async/await?", "async");
            var turn = InterviewTurn.Create(
                sessionId: session.Id,
                userId: userId,
                turnNumber: 1,
                question: question,
                questionGenerationMetadata: new AiCallMetadata("v1", "provider", "model", 10, 20),
                createdAt: createdAt.AddMinutes(1));

            _sessions[session.Id] = session.ToState();
            _turns[session.Id] = new List<InterviewTurn> { turn };
            return session.Id;
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
            _turns[session.Id] = new List<InterviewTurn> { firstTurn };
            return Task.CompletedTask;
        }

        public Task SaveAnswerAsync(
            InterviewSession session,
            InterviewTurn answeredTurn,
            InterviewTurn? nextTurn = null,
            CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            if (_turns.TryGetValue(session.Id, out var turns))
            {
                turns[answeredTurn.TurnNumber - 1] = answeredTurn;
                if (nextTurn != null)
                {
                    turns.Add(nextTurn);
                }
            }

            return Task.CompletedTask;
        }

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            LastUpdatedSessionState = session.ToState();
            AllUpdatedStates.Add(LastUpdatedSessionState);
            _sessions[session.Id] = LastUpdatedSessionState;
            return Task.CompletedTask;
        }

        private sealed class FakeSummarizer : ISessionSummarizer
        {
            private readonly bool _shouldFail;

            public FakeSummarizer(bool shouldFail)
            {
                _shouldFail = shouldFail;
            }

            public Task<SessionSummaryResult> GenerateSummaryAsync(
                SessionSummaryRequest request,
                CancellationToken cancellationToken = default)
            {
                if (_shouldFail)
                {
                    throw new InvalidOperationException("Summarizer configured to fail");
                }

                var summary = new SessionSummaryResult(
                    Summary: "This candidate demonstrated good understanding of core concepts.",
                    AiMetadata: new AiCallMetadata("summary-v1", "AzureOpenAI", "gpt-4o-mini", 150, 80));

                return Task.FromResult(summary);
            }
        }
    }
}
