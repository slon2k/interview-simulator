using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class InterviewErrorContractTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task GetInterview_WhenSessionMissing_ReturnsNotFoundProblemDetailsWithCodeAndTraceId()
    {
        using var client = CreateClientWithStore(new NotFoundInterviewStore());
        var sessionId = Guid.NewGuid();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/interviews/{sessionId}", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("Not found", root.GetProperty("title").GetString());
        Assert.Equal((int)HttpStatusCode.NotFound, root.GetProperty("status").GetInt32());
        Assert.Equal("Interviews.GetInterview.SessionNotFound", root.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task StartInterview_WhenSessionNotCreated_ReturnsConflictProblemDetailsWithCodeAndTraceId()
    {
        var activeSession = CreateActiveSession("github|100");
        using var client = CreateClientWithStore(new SingleSessionStore(activeSession));

        using var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{activeSession.Id}/start", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("Conflict", root.GetProperty("title").GetString());
        Assert.Equal((int)HttpStatusCode.Conflict, root.GetProperty("status").GetInt32());
        Assert.Equal("Interviews.StartInterview.SessionNotCreated", root.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task SubmitAnswer_WhenTurnAlreadyAnswered_ReturnsConflictProblemDetailsWithCodeAndTraceId()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var activeSession = InterviewSession.Create(
            userId: "github|100",
            targetRole: "Software Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 2);
        activeSession.Start(createdAt.AddSeconds(1));

        var answeredTurn = InterviewTurn.Create(
            sessionId: activeSession.Id,
            userId: activeSession.UserId,
            turnNumber: 1,
            question: new InterviewQuestion("Question?", "Topic"),
            createdAt: createdAt.AddSeconds(2));
        answeredTurn.RecordAnswer("Already answered", createdAt.AddSeconds(3));

        using var client = CreateClientWithStore(new SubmitAnswerConflictStore(activeSession, answeredTurn));

        using var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{activeSession.Id}/answers", "github|100", "invited-user");
        request.Content = JsonContent.Create(new { turnNumber = 1, answer = "New answer" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("Conflict", root.GetProperty("title").GetString());
        Assert.Equal((int)HttpStatusCode.Conflict, root.GetProperty("status").GetInt32());
        Assert.Equal("Interviews.InterviewTurn.TurnAlreadyAnswered", root.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task GetInterview_WhenStoreUnavailable_ReturnsServiceUnavailableProblemDetailsWithCodeAndTraceId()
    {
        using var client = CreateClientWithStore(new UnavailableInterviewStore());
        var sessionId = Guid.NewGuid();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/interviews/{sessionId}", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("Service unavailable", root.GetProperty("title").GetString());
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, root.GetProperty("status").GetInt32());
        Assert.Equal("Infrastructure.Cosmos.Interviews.GetSession.Unavailable", root.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    private HttpClient CreateClientWithStore(IInterviewStore interviewStore)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IInterviewStore>(_ => interviewStore);
                services.AddScoped<IQuestionGenerator>(_ => new FakeQuestionGenerator());
            });
        }).CreateClient();
    }

    private static InterviewSession CreateActiveSession(string userId)
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var session = InterviewSession.Create(
            userId: userId,
            targetRole: "Software Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 3);
        session.Start(createdAt.AddSeconds(1));
        return session;
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

    private sealed class NotFoundInterviewStore : IInterviewStore
    {
        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(string userId, IReadOnlyList<InterviewStatus>? statuses, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>(Array.Empty<InterviewSession>());

        public Task<InterviewSession?> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<InterviewSession?>(null);

        public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<InterviewTurn?>(null);

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>(Array.Empty<InterviewTurn>());

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveAnswerAsync(InterviewSession session, InterviewTurn answeredTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class SingleSessionStore(InterviewSession session) : IInterviewStore
    {
        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(string userId, IReadOnlyList<InterviewStatus>? statuses, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>([session]);

        public Task<InterviewSession?> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(session.UserId == userId && session.Id == sessionId ? session : null);

        public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<InterviewTurn?>(null);

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>(Array.Empty<InterviewTurn>());

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveAnswerAsync(InterviewSession session, InterviewTurn answeredTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class SubmitAnswerConflictStore(InterviewSession session, InterviewTurn turn) : IInterviewStore
    {
        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(string userId, IReadOnlyList<InterviewStatus>? statuses, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>([session]);

        public Task<InterviewSession?> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(session.UserId == userId && session.Id == sessionId ? session : null);

        public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(turn.UserId == userId && turn.SessionId == sessionId && turn.TurnNumber == turnNumber ? turn : null);

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>([turn]);

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveAnswerAsync(InterviewSession session, InterviewTurn answeredTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class UnavailableInterviewStore : IInterviewStore
    {
        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(string userId, IReadOnlyList<InterviewStatus>? statuses, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>(Array.Empty<InterviewSession>());

        public Task<InterviewSession?> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromException<InterviewSession?>(new InfrastructureUnavailableException(
                Error.Unavailable(
                    "Infrastructure.Cosmos.Interviews.GetSession.Unavailable",
                    "Interview persistence is temporarily unavailable.")));

        public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<InterviewTurn?>(null);

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>(Array.Empty<InterviewTurn>());

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveAnswerAsync(InterviewSession session, InterviewTurn answeredTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}