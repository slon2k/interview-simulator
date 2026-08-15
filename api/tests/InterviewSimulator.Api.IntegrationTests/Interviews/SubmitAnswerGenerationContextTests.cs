using System.Net;
using System.Net.Http.Json;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.IntegrationTests.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Interviews;

public sealed class SubmitAnswerGenerationContextTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task SubmitAnswer_WhenGeneratingNextQuestion_IncludesLatestAnswerInPreviousTurns()
    {
        var store = new PersistenceLikeInterviewStore();
        var sessionId = store.SeedActiveSessionWithFirstTurn("github|100");
        var generator = new CapturingQuestionGenerator();
        using var client = CreateClient(store, generator);

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{sessionId}/answers",
            "github|100",
            "invited-user");
        request.Content = JsonContent.Create(new { turnNumber = 1, answer = "Answer 1" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var generationRequest = Assert.IsType<GenerateQuestionRequest>(generator.LastRequest);
        var turnOne = Assert.Single(generationRequest.PreviousTurns, turn => turn.TurnNumber == 1);

        Assert.Equal("Answer 1", turnOne.AnswerText);
    }

    [Fact]
    public async Task SubmitAnswer_WhenHistoryExists_PassesOrderedPreviousTurnsIncludingLatestAnswer()
    {
        var store = new PersistenceLikeInterviewStore();
        var sessionId = store.SeedActiveSessionWithAnsweredFirstTurnAndCurrentSecondTurn("github|100");
        var generator = new CapturingQuestionGenerator();
        using var client = CreateClient(store, generator);

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/interviews/{sessionId}/answers",
            "github|100",
            "invited-user");
        request.Content = JsonContent.Create(new { turnNumber = 2, answer = "Answer 2" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var generationRequest = Assert.IsType<GenerateQuestionRequest>(generator.LastRequest);
        Assert.Equal([1, 2], generationRequest.PreviousTurns.Select(turn => turn.TurnNumber).ToArray());
        Assert.Equal("Answer 1", generationRequest.PreviousTurns[0].AnswerText);
        Assert.Equal("Answer 2", generationRequest.PreviousTurns[1].AnswerText);
    }

    private HttpClient CreateClient(PersistenceLikeInterviewStore store, CapturingQuestionGenerator generator)
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

    private sealed class CapturingQuestionGenerator : IQuestionGenerator
    {
        public GenerateQuestionRequest? LastRequest { get; private set; }

        public Task<GeneratedQuestion> GenerateQuestionAsync(GenerateQuestionRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new GeneratedQuestion($"Question {request.TurnNumber}", $"Topic {request.TurnNumber}"));
        }
    }

    private sealed class PersistenceLikeInterviewStore : IInterviewStore
    {
        private readonly Dictionary<Guid, InterviewSessionState> _sessions = new();
        private readonly Dictionary<(Guid SessionId, int TurnNumber), InterviewTurnState> _turns = new();

        public Guid SeedActiveSessionWithFirstTurn(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-15);
            var startedAt = createdAt.AddMinutes(1);

            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 2);
            session.Start(startedAt);

            var firstTurn = InterviewTurn.Create(
                sessionId: session.Id,
                userId: userId,
                turnNumber: 1,
                question: new InterviewQuestion("Question 1", "Topic 1"),
                questionGenerationMetadata: null,
                createdAt: startedAt.AddSeconds(1));

            _sessions[session.Id] = session.ToState();
            _turns[(session.Id, 1)] = firstTurn.ToState();

            return session.Id;
        }

        public Guid SeedActiveSessionWithAnsweredFirstTurnAndCurrentSecondTurn(string userId)
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-20);
            var startedAt = createdAt.AddMinutes(1);
            var firstAnsweredAt = startedAt.AddMinutes(1);

            var session = InterviewSession.Create(
                userId: userId,
                targetRole: "Backend Engineer",
                focusArea: "dotnet",
                seniority: SeniorityLevel.Middle,
                interviewType: InterviewType.Technical,
                createdAt: createdAt,
                questionCount: 3);
            session.Start(startedAt);
            session.RecordAnswer(new SessionResult(new Score(90)), firstAnsweredAt);

            var firstTurn = InterviewTurn.Create(
                sessionId: session.Id,
                userId: userId,
                turnNumber: 1,
                question: new InterviewQuestion("Question 1", "Topic 1"),
                questionGenerationMetadata: null,
                createdAt: startedAt.AddSeconds(1));
            firstTurn.RecordAnswer("Answer 1", firstAnsweredAt);

            var secondTurn = InterviewTurn.Create(
                sessionId: session.Id,
                userId: userId,
                turnNumber: 2,
                question: new InterviewQuestion("Question 2", "Topic 2"),
                questionGenerationMetadata: null,
                createdAt: firstAnsweredAt.AddSeconds(1));

            _sessions[session.Id] = session.ToState();
            _turns[(session.Id, 1)] = firstTurn.ToState();
            _turns[(session.Id, 2)] = secondTurn.ToState();

            return session.Id;
        }

        public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(string userId, IReadOnlyList<InterviewStatus>? statuses, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewSession>>(
                _sessions.Values
                    .Where(state => state.UserId == userId)
                    .Where(state => statuses is null || statuses.Count == 0 || statuses.Contains(state.Status))
                    .Take(limit)
                    .Select(InterviewSession.Restore)
                    .ToArray());

        public Task<InterviewSession?> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
        {
            if (!_sessions.TryGetValue(sessionId, out var state) || !string.Equals(state.UserId, userId, StringComparison.Ordinal))
            {
                return Task.FromResult<InterviewSession?>(null);
            }

            return Task.FromResult<InterviewSession?>(InterviewSession.Restore(state));
        }

        public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
        {
            if (!_turns.TryGetValue((sessionId, turnNumber), out var state) || !string.Equals(state.UserId, userId, StringComparison.Ordinal))
            {
                return Task.FromResult<InterviewTurn?>(null);
            }

            return Task.FromResult<InterviewTurn?>(InterviewTurn.Restore(state));
        }

        public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InterviewTurn>>(
                _turns.Values
                    .Where(state => state.SessionId == sessionId && string.Equals(state.UserId, userId, StringComparison.Ordinal))
                    .OrderBy(state => state.TurnNumber)
                    .Select(InterviewTurn.Restore)
                    .ToArray());

        public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            return Task.CompletedTask;
        }

        public Task StartInterviewAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            _turns[(firstTurn.SessionId, firstTurn.TurnNumber)] = firstTurn.ToState();
            return Task.CompletedTask;
        }

        public Task SaveAnswerAsync(InterviewSession session, InterviewTurn answeredTurn, InterviewTurn? nextTurn = null, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            _turns[(answeredTurn.SessionId, answeredTurn.TurnNumber)] = answeredTurn.ToState();

            if (nextTurn is not null)
            {
                _turns[(nextTurn.SessionId, nextTurn.TurnNumber)] = nextTurn.ToState();
            }

            return Task.CompletedTask;
        }

        public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session.ToState();
            return Task.CompletedTask;
        }
    }
}