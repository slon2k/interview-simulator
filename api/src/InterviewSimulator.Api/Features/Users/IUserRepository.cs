namespace InterviewSimulator.Api.Features.Users;

/// <summary>
/// Defines the contract for a user repository that can retrieve and upsert user documents based on authenticated user profiles.
/// </summary>
public interface IUserRepository
{
    Task<UserDocument?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserDocument?> UpsertAuthenticatedUserAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default);
}