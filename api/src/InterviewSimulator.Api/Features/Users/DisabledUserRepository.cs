namespace InterviewSimulator.Api.Features.Users;

/// <summary>
/// A disabled implementation of <see cref="IUserRepository"/> that always returns null for user lookups and upserts.
/// </summary>
public sealed class DisabledUserRepository : IUserRepository
{
    public Task<UserDocument?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<UserDocument?>(null);
    }

    public Task<UserDocument?> UpsertAuthenticatedUserAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<UserDocument?>(null);
    }
}