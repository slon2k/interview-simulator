namespace InterviewSimulator.Api.Features.Interviews;

public interface IInterviewStore
{
    Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
        string userId,
        IReadOnlyList<InterviewStatus>? statuses,
        int limit,
        CancellationToken cancellationToken = default);

    Task<InterviewSession?> GetSessionAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<InterviewTurn?> GetTurnAsync(
        string userId,
        Guid sessionId,
        int turnNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task CreateSessionAsync(
        InterviewSession session,
        CancellationToken cancellationToken = default);

    Task StartInterviewAsync(
        InterviewSession session,
        InterviewTurn firstTurn,
        CancellationToken cancellationToken = default);

    Task SaveAnswerAsync(
        InterviewSession session,
        InterviewTurn answeredTurn,
        InterviewTurn? nextTurn = null,
        CancellationToken cancellationToken = default);

    Task UpdateSessionAsync(
        InterviewSession session,
        CancellationToken cancellationToken = default);
}
