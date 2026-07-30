using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class GetInterviewsStatusFilterTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task GetInterviews_WithoutStatus_ReturnsAllUserInterviews()
    {
        var sessions = CreateSessionsForFiltering();
        using var client = CreateClientWithStore(sessions);

        using var request = CreateAuthenticatedGetRequest("/api/interviews", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);
        Assert.Equal(2, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetInterviews_WithStatusActive_ReturnsOnlyActiveInterviews()
    {
        var sessions = CreateSessionsForFiltering();
        using var client = CreateClientWithStore(sessions);

        using var request = CreateAuthenticatedGetRequest("/api/interviews?status=active", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);
        Assert.Equal(1, json.RootElement.GetArrayLength());
        Assert.Equal("Active", json.RootElement[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetInterviews_WithStatusCompleted_ReturnsOnlyCompletedInterviews()
    {
        var sessions = CreateSessionsForFiltering();
        using var client = CreateClientWithStore(sessions);

        using var request = CreateAuthenticatedGetRequest("/api/interviews?status=completed", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);
        Assert.Equal(1, json.RootElement.GetArrayLength());
        Assert.Equal("Completed", json.RootElement[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetInterviews_WithInvalidStatus_ReturnsBadRequest()
    {
        var sessions = CreateSessionsForFiltering();
        using var client = CreateClientWithStore(sessions);

        using var request = CreateAuthenticatedGetRequest("/api/interviews?status=invalid", "github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await ReadJsonAsync(response);
        Assert.Equal("Invalid status filter. Allowed values: created, active, completed.", json.RootElement.GetProperty("errors").GetProperty("Status")[0].GetString());
    }

    private HttpClient CreateClientWithStore(IReadOnlyList<InterviewSession> sessions)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IInterviewStore>(_ => new FakeInterviewStore(sessions));
            });
        }).CreateClient();
    }

    private static IReadOnlyList<InterviewSession> CreateSessionsForFiltering()
    {
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);

        var active = InterviewSession.Create(
            userId: "github|100",
            targetRole: "Software Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 5);
        active.Start(createdAt.AddMinutes(1));

        var completed = InterviewSession.Create(
            userId: "github|100",
            targetRole: "Software Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 5);
        completed.Start(createdAt.AddMinutes(1));
        completed.Complete(createdAt.AddMinutes(30));

        var anotherUser = InterviewSession.Create(
            userId: "github|200",
            targetRole: "Software Engineer",
            focusArea: "dotnet",
            seniority: SeniorityLevel.Middle,
            interviewType: InterviewType.Technical,
            createdAt: createdAt,
            questionCount: 5);
        anotherUser.Start(createdAt.AddMinutes(1));

        return [active, completed, anotherUser];
    }

    private static HttpRequestMessage CreateAuthenticatedGetRequest(
        string path,
        string userId,
        string login)
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

    private sealed class FakeInterviewStore(IReadOnlyList<InterviewSession> sessions) : IInterviewStore
    {
        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
            string userId,
            InterviewStatus? status,
            int limit,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = sessions
                .Where(s => s.UserId == userId)
                .Where(s => status is null || s.Status == status)
                .Take(limit)
                .ToArray();

            return Task.FromResult<IReadOnlyList<InterviewSession>>(query);
        }

        public Task<InterviewSession?> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<InterviewSession?>(null);

        public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<InterviewTurn?>(null);

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>(Array.Empty<InterviewTurn>());

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CreateTurnAsync(InterviewSession session, InterviewTurn turn, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateTurnAsync(InterviewSession session, InterviewTurn currentTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
