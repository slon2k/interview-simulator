using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class GetInterviewResponseContractTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task GetInterview_WhenCreated_ReturnsCreatedShapeWithoutCurrentQuestionOrTotalScore()
    {
        var store = new GetInterviewContractStore();
        var sessionId = store.SeedCreatedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/interviews/{sessionId}", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("Created", root.GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("answeredCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("startedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("completedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("currentQuestion").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("totalScore").ValueKind);
    }

    [Fact]
    public async Task GetInterview_WhenActive_ReturnsCurrentQuestionAndNoTotalScore()
    {
        var store = new GetInterviewContractStore();
        var sessionId = store.SeedActiveSessionWithCurrentTurn("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/interviews/{sessionId}", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("Active", root.GetProperty("status").GetString());
        Assert.Equal(1, root.GetProperty("answeredCount").GetInt32());
        Assert.Equal(JsonValueKind.String, root.GetProperty("startedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("completedAt").ValueKind);
        Assert.Equal("Question 2", root.GetProperty("currentQuestion").GetProperty("text").GetString());
        Assert.Equal("Topic 2", root.GetProperty("currentQuestion").GetProperty("topic").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("totalScore").ValueKind);
    }

    [Fact]
    public async Task GetInterview_WhenCompleted_ReturnsTotalScoreAndNoCurrentQuestion()
    {
        var store = new GetInterviewContractStore();
        var sessionId = store.SeedCompletedSession("github|100");
        using var client = CreateClientWithStore(store);

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/interviews/{sessionId}", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("Completed", root.GetProperty("status").GetString());
        Assert.Equal(2, root.GetProperty("answeredCount").GetInt32());
        Assert.Equal(JsonValueKind.String, root.GetProperty("startedAt").ValueKind);
        Assert.Equal(JsonValueKind.String, root.GetProperty("completedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("currentQuestion").ValueKind);
        Assert.Equal(85, root.GetProperty("totalScore").GetInt32());
    }

    private HttpClient CreateClientWithStore(GetInterviewContractStore store)
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

    private sealed class GetInterviewContractStore : IInterviewStore
    {
        private readonly Dictionary<Guid, InterviewSession> _sessions = new();
        private readonly Dictionary<(Guid SessionId, int TurnNumber), InterviewTurn> _turns = new();

        public Guid SeedCreatedSession(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-20);
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

        public Guid SeedActiveSessionWithCurrentTurn(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-20);
            var startedAt = createdAt.AddMinutes(1);
            var answeredAt = startedAt.AddMinutes(1);

            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 3);

            session.Start(startedAt);
            session.RecordAnswer(new SessionResult(new Score(90)), answeredAt);
            _sessions[session.Id] = session;

            var answeredTurn = InterviewTurn.Create(
                sessionId: session.Id,
                userId: userId,
                turnNumber: 1,
                question: new InterviewQuestion("Question 1", "Topic 1"),
                questionGenerationMetadata: null,
                createdAt: startedAt);
            answeredTurn.RecordAnswer("Answer 1", answeredAt);

            var currentTurn = InterviewTurn.Create(
                sessionId: session.Id,
                userId: userId,
                turnNumber: 2,
                question: new InterviewQuestion("Question 2", "Topic 2"),
                questionGenerationMetadata: null,
                createdAt: answeredAt.AddSeconds(1));

            _turns[(session.Id, 1)] = answeredTurn;
            _turns[(session.Id, 2)] = currentTurn;

            return session.Id;
        }

        public Guid SeedCompletedSession(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-30);
            var startedAt = createdAt.AddMinutes(1);
            var completedAt = startedAt.AddMinutes(5);

            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 2);

            session.Start(startedAt);
            session.RecordAnswer(new SessionResult(new Score(80)), startedAt.AddMinutes(1));
            session.RecordAnswer(new SessionResult(new Score(85)), completedAt);

            _sessions[session.Id] = session;
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