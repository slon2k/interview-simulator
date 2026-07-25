using InterviewSimulator.Api.Features.Identity.CurrentUser;

namespace InterviewSimulator.Api.Infrastructure.Identity;

/// <summary>
/// Defines the contract for a user repository that can retrieve and upsert user documents based on authenticated user profiles.
/// </summary>
public interface IUserRepository
{
    Task<CosmosUserDocument?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<CosmosUserDocument?> UpsertAuthenticatedUserAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default);
}