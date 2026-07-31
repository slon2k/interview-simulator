using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class InterviewLifecycleHappyPathTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task InterviewLifecycle_CreateStartSubmitAndRead_WorksEndToEnd()
    {
        var store = new InMemoryInterviewStore();
        var generator = new SequencedQuestionGenerator();
        using var client = CreateClient(store, generator);

        using var createRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/interviews", "github|100", "invited-user");
        createRequest.Content = JsonContent.Create(new
        {
            targetRole = "Backend Engineer",
            focusArea = "dotnet",
            interviewType = "Technical",
            seniorityLevel = "Middle",
            questionCount = 2
        });

        var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createJson = await ReadJsonAsync(createResponse);
        var createdRoot = createJson.RootElement;

        var interviewId = createdRoot.GetProperty("id").GetGuid();
        Assert.Equal("Created", createdRoot.GetProperty("status").GetString());
        Assert.NotNull(createResponse.Headers.Location);
        Assert.Equal($"/api/interviews/{interviewId}", createResponse.Headers.Location!.OriginalString);

        using var startRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/start", "github|100", "invited-user");
        var startResponse = await client.SendAsync(startRequest);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        using var startJson = await ReadJsonAsync(startResponse);
        var startRoot = startJson.RootElement;

        Assert.Equal("Active", startRoot.GetProperty("status").GetString());
        Assert.Equal(0, startRoot.GetProperty("answeredCount").GetInt32());
        Assert.Equal("Question 1", startRoot.GetProperty("currentQuestion").GetProperty("text").GetString());

        using var firstAnswerRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/answers", "github|100", "invited-user");
        firstAnswerRequest.Content = JsonContent.Create(new
        {
            turnNumber = 1,
            answer = "My first answer"
        });

        var firstAnswerResponse = await client.SendAsync(firstAnswerRequest);
        Assert.Equal(HttpStatusCode.OK, firstAnswerResponse.StatusCode);

        using var firstAnswerJson = await ReadJsonAsync(firstAnswerResponse);
        var firstAnswerRoot = firstAnswerJson.RootElement;

        Assert.Equal("Active", firstAnswerRoot.GetProperty("status").GetString());
        Assert.Equal(1, firstAnswerRoot.GetProperty("answeredCount").GetInt32());
        Assert.Equal("Question 2", firstAnswerRoot.GetProperty("currentQuestion").GetProperty("text").GetString());

        using var secondAnswerRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/answers", "github|100", "invited-user");
        secondAnswerRequest.Content = JsonContent.Create(new
        {
            turnNumber = 2,
            answer = "My final answer"
        });

        var secondAnswerResponse = await client.SendAsync(secondAnswerRequest);
        Assert.Equal(HttpStatusCode.OK, secondAnswerResponse.StatusCode);

        using var secondAnswerJson = await ReadJsonAsync(secondAnswerResponse);
        var secondAnswerRoot = secondAnswerJson.RootElement;

        Assert.Equal("Completed", secondAnswerRoot.GetProperty("status").GetString());
        Assert.Equal(2, secondAnswerRoot.GetProperty("answeredCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, secondAnswerRoot.GetProperty("currentQuestion").ValueKind);
        Assert.Equal(JsonValueKind.String, secondAnswerRoot.GetProperty("completedAt").ValueKind);

        using var getInterviewRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/interviews/{interviewId}", "github|100", "invited-user");
        var getInterviewResponse = await client.SendAsync(getInterviewRequest);
        Assert.Equal(HttpStatusCode.OK, getInterviewResponse.StatusCode);

        using var getInterviewJson = await ReadJsonAsync(getInterviewResponse);
        var interviewRoot = getInterviewJson.RootElement;

        Assert.Equal(interviewId, interviewRoot.GetProperty("id").GetGuid());
        Assert.Equal("Completed", interviewRoot.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, interviewRoot.GetProperty("currentQuestion").ValueKind);

        using var listRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/interviews?status=completed", "github|100", "invited-user");
        var listResponse = await client.SendAsync(listRequest);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var listJson = await ReadJsonAsync(listResponse);
        var listRoot = listJson.RootElement;

        Assert.Equal(1, listRoot.GetArrayLength());
        Assert.Equal(interviewId, listRoot[0].GetProperty("id").GetGuid());
        Assert.Equal("Completed", listRoot[0].GetProperty("status").GetString());
    }

    private HttpClient CreateClient(InMemoryInterviewStore store, SequencedQuestionGenerator generator)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IInterviewStore>(_ => store);
                services.AddScoped<IQuestionGenerator>(_ => generator);
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

    private sealed class SequencedQuestionGenerator : IQuestionGenerator
    {
        public Task<GeneratedQuestion> GenerateQuestionAsync(GenerateQuestionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new GeneratedQuestion($"Question {request.TurnNumber}", $"Topic {request.TurnNumber}"));
    }

    private sealed class InMemoryInterviewStore : IInterviewStore
    {
        private readonly Dictionary<Guid, InterviewSession> _sessions = new();
        private readonly Dictionary<(Guid SessionId, int TurnNumber), InterviewTurn> _turns = new();

        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
            string userId,
            IReadOnlyList<InterviewStatus>? statuses,
            int limit,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = _sessions.Values
                .Where(session => session.UserId == userId)
                .Where(session => statuses is null || statuses.Count == 0 || statuses.Contains(session.Status))
                .Take(limit)
                .ToArray();

            return Task.FromResult<IReadOnlyList<InterviewSession>>(result);
        }

        public Task<InterviewSession?> GetSessionAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session is not null && session.UserId == userId ? session : null);
        }

        public Task<InterviewTurn?> GetTurnAsync(
            string userId,
            Guid sessionId,
            int turnNumber,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _turns.TryGetValue((sessionId, turnNumber), out var turn);
            return Task.FromResult(turn is not null && turn.UserId == userId ? turn : null);
        }

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = _turns.Values
                .Where(turn => turn.UserId == userId && turn.SessionId == sessionId)
                .OrderBy(turn => turn.TurnNumber)
                .ToArray();

            return Task.FromResult<IReadOnlyList<InterviewTurn>>(result);
        }

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task StartInterviewAsync(
            InterviewSession session,
            InterviewTurn firstTurn,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions[session.Id] = session;
            _turns[(firstTurn.SessionId, firstTurn.TurnNumber)] = firstTurn;
            return Task.CompletedTask;
        }

        public Task SaveAnswerAsync(
            InterviewSession session,
            InterviewTurn answeredTurn,
            InterviewTurn? nextTurn = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
            cancellationToken.ThrowIfCancellationRequested();
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}