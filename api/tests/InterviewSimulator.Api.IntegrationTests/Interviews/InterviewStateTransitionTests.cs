using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class InterviewStateTransitionTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task StartInterview_WhenSessionMissing_ReturnsNotFoundWithStableCode()
    {
        using var client = CreateClientWithStore(new TransitionStore());

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{Guid.NewGuid()}/start",
            "github|100",
            "invited-user");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        Assert.Equal("Interviews.StartInterview.SessionNotFound", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task SubmitAnswer_WhenSessionNotActive_ReturnsConflictWithStableCode()
    {
        var store = new TransitionStore();
        var sessionId = store.SeedCreatedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{sessionId}/answers",
            "github|100",
            "invited-user");
        request.Content = JsonContent.Create(new { turnNumber = 1, answer = "Answer" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        Assert.Equal("Interviews.SubmitAnswer.SessionNotActive", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task SubmitAnswer_WhenTurnNumberSkipsExpected_ReturnsConflictWithStableCode()
    {
        var store = new TransitionStore();
        var sessionId = store.SeedActiveSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{sessionId}/answers",
            "github|100",
            "invited-user");
        request.Content = JsonContent.Create(new { turnNumber = 2, answer = "Answer" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        Assert.Equal("Interviews.SubmitAnswer.InvalidTurnNumber", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CompleteInterview_WhenSessionNotActive_ReturnsConflictWithStableCode()
    {
        var store = new TransitionStore();
        var sessionId = store.SeedCreatedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{sessionId}/complete",
            "github|100",
            "invited-user");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        Assert.Equal("Interviews.CompleteInterview.SessionNotActive", json.RootElement.GetProperty("code").GetString());
    }

    private HttpClient CreateClientWithStore(TransitionStore store)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IInterviewStore>(_ => store);
                services.AddScoped<IQuestionGenerator>(_ => new FakeQuestionGenerator());
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

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(json);
        return json;
    }

    private sealed class FakeQuestionGenerator : IQuestionGenerator
    {
        public Task<GeneratedQuestion> GenerateQuestionAsync(GenerateQuestionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new GeneratedQuestion("Question", "Topic"));
    }

    private sealed class TransitionStore : IInterviewStore
    {
        private readonly Dictionary<Guid, InterviewSession> _sessions = new();
        private readonly Dictionary<(Guid SessionId, int TurnNumber), InterviewTurn> _turns = new();

        public Guid SeedCreatedSession(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 3);

            _sessions[session.Id] = session;
            return session.Id;
        }

        public Guid SeedActiveSession(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 3);

            session.Start(createdAt.AddSeconds(1));
            _sessions[session.Id] = session;

            var firstTurn = InterviewTurn.Create(
                sessionId: session.Id,
                userId: userId,
                turnNumber: 1,
                question: new InterviewQuestion("Question 1", "Topic 1"),
                questionGenerationMetadata: null,
                createdAt: createdAt.AddSeconds(2));

            _turns[(session.Id, 1)] = firstTurn;
            return session.Id;
        }

        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
            string userId,
            IReadOnlyList<InterviewStatus>? statuses,
            int limit,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>(
                _sessions.Values
                    .Where(session => session.UserId == userId)
                    .Where(session => statuses is null || statuses.Count == 0 || statuses.Contains(session.Status))
                    .Take(limit)
                    .ToArray());

        public Task<InterviewSession?> GetSessionAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session is not null && session.UserId == userId ? session : null);
        }

        public Task<InterviewTurn?> GetTurnAsync(
            string userId,
            Guid sessionId,
            int turnNumber,
            CancellationToken cancellationToken = default)
        {
            _turns.TryGetValue((sessionId, turnNumber), out var turn);
            return Task.FromResult(turn is not null && turn.UserId == userId ? turn : null);
        }

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>(
                _turns.Values
                    .Where(turn => turn.UserId == userId && turn.SessionId == sessionId)
                    .OrderBy(turn => turn.TurnNumber)
                    .ToArray());

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session;
            _turns[(firstTurn.SessionId, firstTurn.TurnNumber)] = firstTurn;
            return Task.CompletedTask;
        }

        public Task SaveAnswerAsync(InterviewSession session, InterviewTurn answeredTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session;
            _turns[(answeredTurn.SessionId, answeredTurn.TurnNumber)] = answeredTurn;

            if (nextTurn is not null)
            {
                _turns[(nextTurn.SessionId, nextTurn.TurnNumber)] = nextTurn;
            }

            return Task.CompletedTask;
        }

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}