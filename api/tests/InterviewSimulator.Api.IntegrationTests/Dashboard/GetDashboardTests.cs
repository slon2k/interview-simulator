using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Dashboard;
using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Dashboard;

public sealed class GetDashboardTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task GetDashboard_ForInvitedUser_ReturnsAggregatesAndScopesQueriesToUser()
    {
        var query = new FakeDashboardQueries();
        using var client = CreateClient(query);

        using var request = CreateAuthenticatedRequest("github|100", "invited-user");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;

        Assert.Equal(2, root.GetProperty("totalCompletedSessions").GetInt32());
        Assert.Equal(80, root.GetProperty("averageScore").GetInt32());

        var trend = root.GetProperty("scoreTrend");
        Assert.Equal(2, trend.GetArrayLength());
        Assert.Equal(70, trend[0].GetProperty("totalScore").GetInt32());
        Assert.Equal(90, trend[1].GetProperty("totalScore").GetInt32());

        var focusArea = Assert.Single(root.GetProperty("scoresByFocusArea").EnumerateArray());
        Assert.Equal("Algorithms", focusArea.GetProperty("focusArea").GetString());
        Assert.Equal(80, focusArea.GetProperty("score").GetInt32());
        Assert.Equal(2, focusArea.GetProperty("sessionCount").GetInt32());

        var interviewType = Assert.Single(root.GetProperty("scoresByInterviewType").EnumerateArray());
        Assert.Equal("Technical", interviewType.GetProperty("interviewType").GetString());

        var dimension = Assert.Single(root.GetProperty("scoresByDimension").EnumerateArray());
        Assert.Equal("clarity", dimension.GetProperty("key").GetString());
        Assert.Equal(80, dimension.GetProperty("score").GetInt32());
        Assert.Equal(2, dimension.GetProperty("sampleCount").GetInt32());

        Assert.Equal(2, root.GetProperty("recentSessions").GetArrayLength());
        Assert.Equal("github|100", query.LastUserId);
        Assert.Equal(1, query.CompletedSessionQueryCount);
        Assert.Equal(1, query.DimensionQueryCount);
    }

    [Fact]
    public async Task GetDashboard_WhenAnonymous_ReturnsUnauthorized()
    {
        using var client = CreateClient(new FakeDashboardQueries());

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_WhenAuthenticatedButNotInvited_ReturnsForbidden()
    {
        using var client = CreateClient(new FakeDashboardQueries());
        using var request = CreateAuthenticatedRequest("github|300", "non-invited-user");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateClient(FakeDashboardQueries query)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IDashboardQueries>(_ => query);
            });
        }).CreateClient();
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(string userId, string login)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
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

    private sealed class FakeDashboardQueries : IDashboardQueries
    {
        private readonly Guid _olderSessionId = Guid.NewGuid();
        private readonly Guid _newerSessionId = Guid.NewGuid();

        public string? LastUserId { get; private set; }

        public int CompletedSessionQueryCount { get; private set; }

        public int DimensionQueryCount { get; private set; }

        public Task<IReadOnlyCollection<DashboardSessionProjection>> GetCompletedSessionProjectionsAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            RecordQuery(userId);
            CompletedSessionQueryCount++;

            if (userId != "github|100")
            {
                return Task.FromResult<IReadOnlyCollection<DashboardSessionProjection>>([]);
            }

            return Task.FromResult<IReadOnlyCollection<DashboardSessionProjection>>(
            [
                new(_olderSessionId, "Backend Engineer", "Algorithms", InterviewType.Technical, InterviewStatus.Completed, DateTimeOffset.Parse("2026-08-01T10:00:00Z"), 70),
                new(_newerSessionId, "Backend Engineer", "Algorithms", InterviewType.Technical, InterviewStatus.Completed, DateTimeOffset.Parse("2026-08-02T10:00:00Z"), 90),
                new(Guid.NewGuid(), "Backend Engineer", "Algorithms", InterviewType.Technical, InterviewStatus.Active, null, 100),
            ]);
        }

        public Task<IReadOnlyCollection<DashboardDimensionProjection>> GetEvaluatedTurnDimensionProjectionsAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            RecordQuery(userId);
            DimensionQueryCount++;

            if (userId != "github|100")
            {
                return Task.FromResult<IReadOnlyCollection<DashboardDimensionProjection>>([]);
            }

            return Task.FromResult<IReadOnlyCollection<DashboardDimensionProjection>>(
            [
                new(_olderSessionId, "clarity", "Clarity", 70),
                new(_newerSessionId, "clarity", "Clarity", 90),
            ]);
        }

        private void RecordQuery(string userId)
        {
            LastUserId = userId;
        }
    }
}