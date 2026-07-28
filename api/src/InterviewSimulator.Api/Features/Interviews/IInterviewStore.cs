namespace InterviewSimulator.Api.Features.Interviews;

public interface IInterviewStore
{
    Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
        string userId,
        InterviewStatus? status,
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

    Task CreateInterviewAsync(
        InterviewSession session,
        InterviewTurn firstTurn,
        CancellationToken cancellationToken = default);

    Task SaveAnswerSubmissionAsync(
        InterviewSession session,
        InterviewTurn answeredTurn,
        InterviewTurn? nextTurn,
        CancellationToken cancellationToken = default);

    Task SaveSessionAsync(
        InterviewSession session,
        CancellationToken cancellationToken = default);
}
