using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace InterviewSimulator.Api.IntegrationTests.Auth;

public sealed class InviteOnlyAuthorizationTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Me_WhenAnonymous_ReturnsUnauthenticatedUser()
    {
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.False(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isInvited").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("userId").ValueKind);
    }

    [Fact]
    public async Task Smoke_WhenAnonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/smoke");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.Equal("unauthorized", json.RootElement.GetProperty("error").GetString());
        Assert.Equal("Authentication is required.", json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Smoke_WhenAuthenticatedButNotInvited_ReturnsForbidden()
    {
        using var request = CreateAuthenticatedGetRequest(
            "/api/auth/smoke",
            userId: "github|300",
            login: "non-invited-user");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.Equal("forbidden", json.RootElement.GetProperty("error").GetString());
        Assert.Equal("You do not have access to this resource.", json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Smoke_WhenAuthenticatedAndInvited_ReturnsOk()
    {
        using var request = CreateAuthenticatedGetRequest(
            "/api/auth/smoke",
            userId: "github|100",
            login: "invited-user");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.Equal("authenticated", json.RootElement.GetProperty("status").GetString());
        Assert.Equal("github|100", json.RootElement.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task Smoke_WhenAuthenticatedAndAdmin_ReturnsOk()
    {
        using var request = CreateAuthenticatedGetRequest(
            "/api/auth/smoke",
            userId: "github|200",
            login: "admin-user");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.Equal("authenticated", json.RootElement.GetProperty("status").GetString());
        Assert.Equal("github|200", json.RootElement.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task Me_WhenAuthenticatedButNotInvited_ReturnsRealAccessStatus()
    {
        using var request = CreateAuthenticatedGetRequest(
            "/api/me",
            userId: "github|300",
            login: "non-invited-user");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.True(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isInvited").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isAdmin").GetBoolean());
        Assert.Equal("github|300", json.RootElement.GetProperty("userId").GetString());
        Assert.Equal("github", json.RootElement.GetProperty("identityProvider").GetString());
        Assert.Equal("non-invited-user", json.RootElement.GetProperty("githubLogin").GetString());
    }

    [Fact]
    public async Task Me_WhenAuthenticatedAndInvited_ReturnsRealAccessStatus()
    {
        using var request = CreateAuthenticatedGetRequest(
            "/api/me",
            userId: "github|100",
            login: "invited-user");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.True(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.True(json.RootElement.GetProperty("isInvited").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isAdmin").GetBoolean());
        Assert.Equal("github|100", json.RootElement.GetProperty("userId").GetString());
        Assert.Equal("github", json.RootElement.GetProperty("identityProvider").GetString());
        Assert.Equal("invited-user", json.RootElement.GetProperty("githubLogin").GetString());
    }

    [Fact]
    public async Task Me_WhenAuthenticatedAndAdmin_ReturnsAdminAsInvited()
    {
        using var request = CreateAuthenticatedGetRequest(
            "/api/me",
            userId: "github|200",
            login: "admin-user");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.True(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.True(json.RootElement.GetProperty("isInvited").GetBoolean());
        Assert.True(json.RootElement.GetProperty("isAdmin").GetBoolean());
        Assert.Equal("github|200", json.RootElement.GetProperty("userId").GetString());
        Assert.Equal("github", json.RootElement.GetProperty("identityProvider").GetString());
        Assert.Equal("admin-user", json.RootElement.GetProperty("githubLogin").GetString());
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
}