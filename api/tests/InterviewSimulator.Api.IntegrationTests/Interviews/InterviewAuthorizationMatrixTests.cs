using System.Net;
using System.Net.Http.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class InterviewAuthorizationMatrixTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [MemberData(nameof(InterviewRouteCases))]
    public async Task InterviewEndpoint_WhenAnonymous_ReturnsUnauthorized(
        HttpMethod method,
        string path,
        object? payload)
    {
        using var request = CreateRequest(method, path, payload);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InterviewRouteCases))]
    public async Task InterviewEndpoint_WhenAuthenticatedButNotInvited_ReturnsForbidden(
        HttpMethod method,
        string path,
        object? payload)
    {
        using var request = CreateRequest(method, path, payload);
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "github|300");
        request.Headers.Add(TestAuthHandler.LoginHeaderName, "non-invited-user");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InterviewRouteCases))]
    public async Task InterviewEndpoint_WhenInvited_ReturnsNonAuthFailure(
        HttpMethod method,
        string path,
        object? payload)
    {
        using var request = CreateRequest(method, path, payload);
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "github|100");
        request.Headers.Add(TestAuthHandler.LoginHeaderName, "invited-user");

        var response = await _client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public static TheoryData<HttpMethod, string, object?> InterviewRouteCases => new()
    {
        {
            HttpMethod.Get,
            "/api/interviews",
            null
        },
        {
            HttpMethod.Post,
            "/api/interviews",
            new
            {
                targetRole = "Backend Engineer",
                focusArea = "dotnet",
                interviewType = "Technical",
                seniorityLevel = "Middle",
                questionCount = 2
            }
        },
        {
            HttpMethod.Get,
            "/api/interviews/11111111-1111-1111-1111-111111111111",
            null
        },
        {
            HttpMethod.Post,
            "/api/interviews/11111111-1111-1111-1111-111111111111/start",
            null
        },
        {
            HttpMethod.Post,
            "/api/interviews/11111111-1111-1111-1111-111111111111/answers",
            new
            {
                turnNumber = 1,
                answer = "My answer"
            }
        },
        {
            HttpMethod.Post,
            "/api/interviews/11111111-1111-1111-1111-111111111111/complete",
            null
        }
    };

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, object? payload)
    {
        var request = new HttpRequestMessage(method, path);

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return request;
    }
}