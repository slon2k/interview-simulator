using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class CompleteInterviewHappyPathTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task CompleteInterview_WhenActive_ReturnsNoContentAndPersistsCompletedState()
    {
        var store = new CompleteInterviewStore();
        var interviewId = store.SeedActiveSession("github|100");
        using var client = CreateClientWithStore(store);

        using var completeRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{interviewId}/complete",
            "github|100",
            "invited-user");

        var completeResponse = await client.SendAsync(completeRequest);

        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);
        Assert.True(store.UpdateCalled);

        var persisted = Assert.IsType<InterviewSessionState>(store.LastUpdatedSessionState);
        Assert.Equal(InterviewStatus.Completed, persisted.Status);
        Assert.NotNull(persisted.CompletedAt);

        using var getRequest = CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"/api/interviews/{interviewId}",
            "github|100",
            "invited-user");

        var getResponse = await client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var getJson = await ReadJsonAsync(getResponse);
        var root = getJson.RootElement;

        Assert.Equal("Completed", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.String, root.GetProperty("completedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("currentQuestion").ValueKind);
    }

    private HttpClient CreateClientWithStore(CompleteInterviewStore store)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IInterviewStore>(_ => store);
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

    private sealed class CompleteInterviewStore : IInterviewStore
    {
        private readonly Dictionary<Guid, InterviewSessionState> _sessions = new();

        public bool UpdateCalled { get; private set; }

        public InterviewSessionState? LastUpdatedSessionState { get; private set; }

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
            => Task.FromResult<InterviewTurn?>(null);

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>(Array.Empty<InterviewTurn>());

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            return Task.CompletedTask;
        }

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            return Task.CompletedTask;
        }

        public Task SaveAnswerAsync(InterviewSession session, InterviewTurn answeredTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default)
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
    }
}