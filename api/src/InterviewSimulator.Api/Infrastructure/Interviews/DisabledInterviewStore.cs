using InterviewSimulator.Api.Features.Interviews;

namespace InterviewSimulator.Api.Infrastructure.Interviews;

public class DisabledInterviewStore : IInterviewStore
{
    public Task CreateSessionAsync(InterviewSession session, InterviewTurn firstTurn, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<InterviewSession?> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<InterviewSession?>(null);
    }

    public Task<InterviewTurn?> GetTurnAsync(string userId, Guid sessionId, int turnNumber, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<InterviewTurn?>(null);
    }

    public Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(string userId, InterviewStatus? status, int limit, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<InterviewSession>>(Array.Empty<InterviewSession>());
    }

    public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<InterviewTurn>>(Array.Empty<InterviewTurn>());
    }

    public Task UpdateTurnAsync(InterviewSession session, InterviewTurn currentTurn, InterviewTurn? nextTurn, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UpdateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CreateSessionAsync(InterviewSession session, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CreateTurnAsync(InterviewSession session, InterviewTurn turn, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}