using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class InterviewAiFlowEndToEndTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task InterviewAiFlow_CreateStartAnswerWithEvaluationCompleteAndVerifyMetadata_WorksEndToEnd()
    {
        var store = new InMemoryInterviewStore();
        var generator = new MetadataRecordingQuestionGenerator();
        var evaluator = new HardcodedAnswerEvaluator();
        
        using var client = CreateClient(store, generator, evaluator);

        // Create interview
        using var createRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/interviews", "github|100", "invited-user");
        createRequest.Content = JsonContent.Create(new
        {
            targetRole = "Backend Engineer",
            focusArea = "dotnet",
            interviewType = "Technical",
            seniorityLevel = "Middle",
            questionCount = 3
        });

        var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createJson = await ReadJsonAsync(createResponse);
        var createdRoot = createJson.RootElement;
        var interviewId = createdRoot.GetProperty("id").GetGuid();

        // Start interview
        using var startRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/start", "github|100", "invited-user");
        var startResponse = await client.SendAsync(startRequest);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        // Verify first turn has generation metadata with prompt version
        var turn1 = await store.GetTurnAsync("github|100", interviewId, 1);
        Assert.NotNull(turn1);
        Assert.NotNull(turn1.QuestionGenerationMetadata);
        Assert.Equal(PromptVersions.HardcodedQuestionGeneration, turn1.QuestionGenerationMetadata.PromptVersion);

        // Submit first answer
        using var firstAnswerRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/answers", "github|100", "invited-user");
        firstAnswerRequest.Content = JsonContent.Create(new
        {
            turnNumber = 1,
            answer = "Answer to technical question 1"
        });

        var firstAnswerResponse = await client.SendAsync(firstAnswerRequest);
        Assert.Equal(HttpStatusCode.OK, firstAnswerResponse.StatusCode);

        using var firstAnswerJson = await ReadJsonAsync(firstAnswerResponse);
        var firstAnswerRoot = firstAnswerJson.RootElement;
        Assert.Equal(1, firstAnswerRoot.GetProperty("answeredCount").GetInt32());

        // Verify turn 1 has evaluation metadata and dimensions
        var turn1Updated = await store.GetTurnAsync("github|100", interviewId, 1);
        Assert.NotNull(turn1Updated);
        Assert.NotNull(turn1Updated.Evaluation);
        Assert.NotNull(turn1Updated.AnswerEvaluationMetadata);
        Assert.Equal(PromptVersions.HardcodedAnswerEvaluation, turn1Updated.AnswerEvaluationMetadata.PromptVersion);
        
        // Verify evaluation structure
        var evaluation1 = turn1Updated.Evaluation;
        Assert.Equal(80, evaluation1.OverallScore.Value);
        Assert.Equal(4, evaluation1.Dimensions.Count); // Technical has 4 dimensions
        Assert.All(evaluation1.Dimensions, dimension =>
        {
            Assert.Equal(80, dimension.Score.Value);
            Assert.NotEmpty(dimension.Feedback.Text);
        });

        // Verify turn 2 was created with generation metadata
        var turn2 = await store.GetTurnAsync("github|100", interviewId, 2);
        Assert.NotNull(turn2);
        Assert.NotNull(turn2.QuestionGenerationMetadata);
        Assert.Equal(PromptVersions.HardcodedQuestionGeneration, turn2.QuestionGenerationMetadata.PromptVersion);

        // Submit second answer
        using var secondAnswerRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/answers", "github|100", "invited-user");
        secondAnswerRequest.Content = JsonContent.Create(new
        {
            turnNumber = 2,
            answer = "Answer to technical question 2"
        });

        var secondAnswerResponse = await client.SendAsync(secondAnswerRequest);
        Assert.Equal(HttpStatusCode.OK, secondAnswerResponse.StatusCode);

        using var secondAnswerJson = await ReadJsonAsync(secondAnswerResponse);
        var secondAnswerRoot = secondAnswerJson.RootElement;
        Assert.Equal(2, secondAnswerRoot.GetProperty("answeredCount").GetInt32());

        // Verify turn 2 has evaluation metadata
        var turn2Updated = await store.GetTurnAsync("github|100", interviewId, 2);
        Assert.NotNull(turn2Updated);
        Assert.NotNull(turn2Updated.Evaluation);
        Assert.NotNull(turn2Updated.AnswerEvaluationMetadata);
        Assert.Equal(PromptVersions.HardcodedAnswerEvaluation, turn2Updated.AnswerEvaluationMetadata.PromptVersion);
        Assert.Equal(4, turn2Updated.Evaluation.Dimensions.Count);

        // Verify turn 3 was created with generation metadata
        var turn3 = await store.GetTurnAsync("github|100", interviewId, 3);
        Assert.NotNull(turn3);
        Assert.NotNull(turn3.QuestionGenerationMetadata);
        Assert.Equal(PromptVersions.HardcodedQuestionGeneration, turn3.QuestionGenerationMetadata.PromptVersion);

        // Submit third answer (final)
        using var thirdAnswerRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/answers", "github|100", "invited-user");
        thirdAnswerRequest.Content = JsonContent.Create(new
        {
            turnNumber = 3,
            answer = "Answer to technical question 3"
        });

        var thirdAnswerResponse = await client.SendAsync(thirdAnswerRequest);
        Assert.Equal(HttpStatusCode.OK, thirdAnswerResponse.StatusCode);

        using var thirdAnswerJson = await ReadJsonAsync(thirdAnswerResponse);
        var thirdAnswerRoot = thirdAnswerJson.RootElement;
        Assert.Equal("Completed", thirdAnswerRoot.GetProperty("status").GetString());
        Assert.Equal(3, thirdAnswerRoot.GetProperty("answeredCount").GetInt32());

        // Verify turn 3 has evaluation metadata and interview is completed
        var turn3Updated = await store.GetTurnAsync("github|100", interviewId, 3);
        Assert.NotNull(turn3Updated);
        Assert.NotNull(turn3Updated.Evaluation);
        Assert.NotNull(turn3Updated.AnswerEvaluationMetadata);
        Assert.Equal(PromptVersions.HardcodedAnswerEvaluation, turn3Updated.AnswerEvaluationMetadata.PromptVersion);
        Assert.Equal(4, turn3Updated.Evaluation.Dimensions.Count);

        // Verify final session state
        var session = await store.GetSessionAsync("github|100", interviewId);
        Assert.NotNull(session);
        Assert.Equal(InterviewStatus.Completed, session.Status);
        Assert.Equal(3, session.AnsweredCount);
        Assert.NotNull(session.CompletedAt);
    }

    [Fact]
    public async Task CompletedInterview_CanBeFoundInHistoryAndReadWithSummaryAndDetails()
    {
        var store = new InMemoryInterviewStore();
        using var client = CreateClient(store, new MetadataRecordingQuestionGenerator(), new HardcodedAnswerEvaluator());

        using var createRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/interviews", "github|100", "invited-user");
        createRequest.Content = JsonContent.Create(new
        {
            targetRole = "Backend Engineer",
            focusArea = "dotnet",
            interviewType = "Technical",
            seniorityLevel = "Middle",
            questionCount = 1
        });

        using var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdJson = await ReadJsonAsync(createResponse);
        var interviewId = createdJson.RootElement.GetProperty("id").GetGuid();

        using var startRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/start", "github|100", "invited-user");
        var startResponse = await client.SendAsync(startRequest);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        using var answerRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/interviews/{interviewId}/answers", "github|100", "invited-user");
        answerRequest.Content = JsonContent.Create(new { turnNumber = 1, answer = "A complete answer" });
        var answerResponse = await client.SendAsync(answerRequest);
        Assert.Equal(HttpStatusCode.OK, answerResponse.StatusCode);

        using var historyRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/interviews?status=completed", "github|100", "invited-user");
        var historyResponse = await client.SendAsync(historyRequest);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        using var historyJson = await ReadJsonAsync(historyResponse);
        var history = historyJson.RootElement.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == interviewId);
        Assert.Equal("Completed", history.GetProperty("status").GetString());

        using var detailsRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/interviews/{interviewId}/details", "github|100", "invited-user");
        var detailsResponse = await client.SendAsync(detailsRequest);
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        using var detailsJson = await ReadJsonAsync(detailsResponse);
        var details = detailsJson.RootElement;
        Assert.Equal("Completed", details.GetProperty("status").GetString());
        Assert.Equal(1, details.GetProperty("turns").GetArrayLength());
        Assert.Equal("A complete answer", details.GetProperty("turns")[0].GetProperty("answer").GetProperty("text").GetString());
        Assert.NotEqual(JsonValueKind.Null, details.GetProperty("summary").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(details.GetProperty("summary").GetProperty("text").GetString()));
    }

    private HttpClient CreateClient(IInterviewStore interviewStore, IQuestionGenerator questionGenerator, IAnswerEvaluator answerEvaluator)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IInterviewStore>(_ => interviewStore);
                services.AddScoped<IQuestionGenerator>(_ => questionGenerator);
                services.AddScoped<IAnswerEvaluator>(_ => answerEvaluator);
            });
        }).CreateClient();
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string path, string userId, string login)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, userId);
        request.Headers.Add(TestAuthHandler.LoginHeaderName, login);
        return request;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        ArgumentNullException.ThrowIfNull(json);
        return json;
    }

    /// <summary>
    /// Question generator that records AI metadata with prompt versions, matching hardcoded behavior.
    /// </summary>
    private sealed class MetadataRecordingQuestionGenerator : IQuestionGenerator
    {
        public Task<GeneratedQuestion> GenerateQuestionAsync(GenerateQuestionRequest request, CancellationToken cancellationToken = default)
        {
            var aiMetadata = new AiCallMetadata(
                PromptVersion: PromptVersions.HardcodedQuestionGeneration,
                Provider: AiProviders.Hardcoded,
                Model: null,
                PromptTokens: null,
                CompletionTokens: null);

            return Task.FromResult(new GeneratedQuestion(
                Text: $"Question {request.TurnNumber}",
                Topic: request.FocusArea,
                AiMetadata: aiMetadata
            ));
        }
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
