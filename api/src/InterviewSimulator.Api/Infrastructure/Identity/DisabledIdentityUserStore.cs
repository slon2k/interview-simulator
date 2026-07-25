using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.CurrentUser;

namespace InterviewSimulator.Api.Infrastructure.Identity;

/// <summary>
/// A disabled implementation of <see cref="IUserRepository"/> that always returns null for user lookups and upserts.
/// </summary>
public sealed class DisabledIdentityUserStore : IUserRepository, IUserAccessReader
{
    public Task<CosmosUserDocument?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<CosmosUserDocument?>(null);
    }

    public Task<CosmosUserDocument?> UpsertAuthenticatedUserAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<CosmosUserDocument?>(null);
    }

    public Task<UserAccessSnapshot?> GetAccessByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<UserAccessSnapshot?>(null);
    }
}