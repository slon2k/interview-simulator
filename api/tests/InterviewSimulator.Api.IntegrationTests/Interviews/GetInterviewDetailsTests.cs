using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class GetInterviewDetailsTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task GetDetails_WhenCompleted_ReturnsOrderedTurnsEvaluationsSummaryAndTotalScore()
    {
        var store = new DetailStore();
        var sessionId = store.SeedCompletedSession("github|100", includeSummary: true);
        using var client = CreateClientWithStore(store);

        using var response = await client.SendAsync(CreateAuthenticatedRequest(
            $"/api/interviews/{sessionId}/details", "github|100", "invited-user"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;
        var turns = root.GetProperty("turns");

        Assert.Equal("Completed", root.GetProperty("status").GetString());
        Assert.Equal(85, root.GetProperty("totalScore").GetInt32());
        Assert.Equal("Strong interview.", root.GetProperty("summary").GetProperty("text").GetString());
        Assert.Equal("2026-08-16T09:00:00+00:00", root.GetProperty("summary").GetProperty("createdAt").GetString());
        Assert.Equal(2, turns.GetArrayLength());
        Assert.Equal(1, turns[0].GetProperty("turnNumber").GetInt32());
        Assert.Equal(2, turns[1].GetProperty("turnNumber").GetInt32());
        Assert.Equal("Question 1", turns[0].GetProperty("question").GetProperty("text").GetString());
        Assert.Equal("Answer 1", turns[0].GetProperty("answer").GetProperty("text").GetString());
        Assert.Equal(
            "2026-08-16T08:56:00+00:00",
            turns[0].GetProperty("createdAt").GetString());
        Assert.Equal(
            "2026-08-16T08:56:10+00:00",
            turns[0].GetProperty("answer").GetProperty("createdAt").GetString());
        Assert.Equal(80, turns[0].GetProperty("evaluation").GetProperty("overallScore").GetInt32());
        Assert.Equal("Good answer.", turns[0].GetProperty("evaluation").GetProperty("overallFeedback").GetString());
        Assert.Equal(1, turns[0].GetProperty("evaluation").GetProperty("dimensions").GetArrayLength());
        Assert.Equal(75, turns[0].GetProperty("evaluation").GetProperty("dimensions")[0].GetProperty("score").GetInt32());
        Assert.False(root.TryGetProperty("summaryAi", out _));
    }

    [Fact]
    public async Task GetDetails_WhenActive_HidesEvaluationsAndTotalScore()
    {
        var store = new DetailStore();
        var sessionId = store.SeedActiveSession("github|100");
        using var client = CreateClientWithStore(store);

        using var response = await client.SendAsync(CreateAuthenticatedRequest(
            $"/api/interviews/{sessionId}/details", "github|100", "invited-user"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("Active", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("totalScore").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("turns")[0].GetProperty("evaluation").ValueKind);
        Assert.Equal("Answer 1", root.GetProperty("turns")[0].GetProperty("answer").GetProperty("text").GetString());
    }

    [Fact]
    public async Task GetDetails_WhenCreated_ReturnsEmptyTurnsAndNoScore()
    {
        var store = new DetailStore();
        var sessionId = store.SeedCreatedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var response = await client.SendAsync(CreateAuthenticatedRequest(
            $"/api/interviews/{sessionId}/details", "github|100", "invited-user"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal(JsonValueKind.Null, root.GetProperty("totalScore").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("summary").ValueKind);
        Assert.Empty(root.GetProperty("turns").EnumerateArray());
    }

    [Fact]
    public async Task GetDetails_WhenSessionMissing_ReturnsNotFoundWithoutLoadingTurns()
    {
        var store = new DetailStore();
        using var client = CreateClientWithStore(store);

        using var response = await client.SendAsync(CreateAuthenticatedRequest(
            $"/api/interviews/{Guid.NewGuid()}/details", "github|100", "invited-user"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, store.ListTurnsCalls);
    }

    [Fact]
    public async Task GetDetails_WhenUserDoesNotOwnSession_ReturnsNotFound()
    {
        var store = new DetailStore();
        var sessionId = store.SeedCompletedSession("github|200", includeSummary: false);
        using var client = CreateClientWithStore(store);

        using var response = await client.SendAsync(CreateAuthenticatedRequest(
            $"/api/interviews/{sessionId}/details", "github|100", "invited-user"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, store.ListTurnsCalls);
    }

    [Fact]
    public async Task GetDetails_UsesAuthenticatedPartitionAndListQueryOnly()
    {
        var store = new DetailStore();
        var sessionId = store.SeedCompletedSession("github|100", includeSummary: false);
        using var client = CreateClientWithStore(store);

        using var response = await client.SendAsync(CreateAuthenticatedRequest(
            $"/api/interviews/{sessionId}/details", "github|100", "invited-user"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("github|100", store.LastSessionUserId);
        Assert.Equal("github|100", store.LastTurnsUserId);
        Assert.Equal(sessionId, store.LastTurnsSessionId);
        Assert.Equal(0, store.GetTurnCalls);
    }

    private HttpClient CreateClientWithStore(DetailStore store) => factory.WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services => services.AddScoped<IInterviewStore>(_ => store));
    }).CreateClient();

    private static HttpRequestMessage CreateAuthenticatedRequest(string path, string userId, string login)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
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

    private sealed class DetailStore : IInterviewStore
    {
        private readonly Dictionary<Guid, InterviewSession> sessions = new();
        private readonly Dictionary<Guid, List<InterviewTurn>> turns = new();

        public int GetTurnCalls { get; private set; }
        public int ListTurnsCalls { get; private set; }
        public string? LastSessionUserId { get; private set; }
        public string? LastTurnsUserId { get; private set; }
        public Guid LastTurnsSessionId { get; private set; }

        public Guid SeedCreatedSession(string userId)
        {
            var session = InterviewSession.Create(userId, "Backend Engineer", "dotnet", SeniorityLevel.Middle, InterviewType.Technical, DateTimeOffset.UtcNow.AddMinutes(-10), 2);
            sessions[session.Id] = session;
            return session.Id;
        }

        public Guid SeedActiveSession(string userId)
        {
            var createdAt = new DateTimeOffset(2026, 8, 16, 8, 55, 0, TimeSpan.Zero);
            var session = InterviewSession.Create(userId, "Backend Engineer", "dotnet", SeniorityLevel.Middle, InterviewType.Technical, createdAt, 2);
            session.Start(createdAt.AddMinutes(1));
            session.RecordAnswer(new SessionResult(new Score(80)), createdAt.AddMinutes(2));
            sessions[session.Id] = session;
            AddTurn(session, 1, answered: true, evaluated: true, createdAt.AddMinutes(1));
            AddTurn(session, 2, answered: false, evaluated: false, createdAt.AddMinutes(2));
            return session.Id;
        }

        public Guid SeedCompletedSession(string userId, bool includeSummary)
        {
            var createdAt = new DateTimeOffset(2026, 8, 16, 8, 55, 0, TimeSpan.Zero);
            var session = InterviewSession.Create(userId, "Backend Engineer", "dotnet", SeniorityLevel.Middle, InterviewType.Technical, createdAt, 2);
            session.Start(createdAt.AddMinutes(1));
            session.RecordAnswer(new SessionResult(new Score(80)), createdAt.AddMinutes(2));
            session.RecordAnswer(new SessionResult(new Score(85)), createdAt.AddMinutes(3));
            if (includeSummary)
            {
                session.RecordSummary(new InterviewSummary("Strong interview.", new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero)), new AiCallMetadata("summary-v1", "test", "test-model", 10, 20), createdAt.AddMinutes(4));
            }
            sessions[session.Id] = session;
            AddTurn(session, 2, answered: true, evaluated: true, createdAt.AddMinutes(2));
            AddTurn(session, 1, answered: true, evaluated: true, createdAt.AddMinutes(1));
            return session.Id;
        }

        private void AddTurn(InterviewSession session, int turnNumber, bool answered, bool evaluated, DateTimeOffset createdAt)
        {
            var turn = InterviewTurn.Create(session.Id, session.UserId, turnNumber, new InterviewQuestion($"Question {turnNumber}", $"Topic {turnNumber}"), null, createdAt);
            if (answered)
            {
                turn.RecordAnswer($"Answer {turnNumber}", createdAt.AddSeconds(10));
            }
            if (evaluated)
            {
                turn.RecordEvaluation(new AnswerEvaluation(new Score(turnNumber == 1 ? 80 : 90), new Feedback("Good answer."), [new EvaluationDimension("clarity", "Clarity", new Score(75), new Feedback("Clear."))]), null, createdAt.AddSeconds(20));
            }
            if (!turns.TryGetValue(session.Id, out var sessionTurns))
            {
                sessionTurns = [];
                turns[session.Id] = sessionTurns;
            }
            sessionTurns.Add(turn);
        }

        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(string userId, IReadOnlyList<InterviewStatus>? statuses, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>([]);

        public Task<InterviewSession?> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
        {
            LastSessionUserId = userId;
            return Task.FromResult(sessions.TryGetValue(sessionId, out var session) && session.UserId == userId ? session : null);
        }

        public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
        {
            GetTurnCalls++;
            return Task.FromResult<InterviewTurn?>(null);
        }

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
        {
            ListTurnsCalls++;
            LastTurnsUserId = userId;
            LastTurnsSessionId = sessionId;
            return Task.FromResult<IReadOnlyList<InterviewTurn>>(turns.GetValueOrDefault(sessionId, []).Where(turn => turn.UserId == userId).OrderBy(turn => turn.TurnNumber).ToArray());
        }

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveAnswerAsync(InterviewSession session, InterviewTurn answeredTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
