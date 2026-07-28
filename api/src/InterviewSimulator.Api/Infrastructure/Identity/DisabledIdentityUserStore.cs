using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.Profile;

namespace InterviewSimulator.Api.Infrastructure.Identity;

/// <summary>
/// No-op identity store used when Cosmos persistence is disabled.
/// Profile writes are dropped and access lookups always return null.
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