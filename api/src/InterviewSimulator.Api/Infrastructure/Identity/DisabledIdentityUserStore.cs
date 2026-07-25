using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.CurrentUser;

namespace InterviewSimulator.Api.Infrastructure.Identity;

/// <summary>
/// A disabled implementation of <see cref="IUserRepository"/> that always returns null for user lookups and upserts.
/// </summary>
public sealed class DisabledIdentityUserStore : IUserProfileStore, IUserAccessReader
{
    public Task UpsertAuthenticatedUserProfileAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<UserAccessSnapshot?> GetAccessByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<UserAccessSnapshot?>(null);
    }
}