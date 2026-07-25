using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.CurrentUser;
using InterviewSimulator.Api.Infrastructure.Data;

namespace InterviewSimulator.Api.Infrastructure.Identity;

public sealed class CosmosIdentityUserStore(IRepository<CosmosUserDocument> repository) : IUserRepository, IUserAccessReader
{
    public async Task<CosmosUserDocument?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetByIdAsync(userId, userId, cancellationToken);
    }

    public async Task<CosmosUserDocument?> UpsertAuthenticatedUserAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await GetByUserIdAsync(profile.UserId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var userDocument = CreateOrUpdateUserDocument(existingUser, profile, now);
        return await repository.UpsertAsync(userDocument, profile.UserId, cancellationToken: cancellationToken);
    }

    private static CosmosUserDocument CreateOrUpdateUserDocument(CosmosUserDocument? document, AuthenticatedUserProfile profile, DateTimeOffset now)
    {
        if (document is not null)
        {
            document.Provider = profile.Provider;
            document.ProviderUserId = profile.ProviderUserId;
            document.GithubLogin = profile.GithubLogin;
            document.DisplayName = profile.DisplayName;
            document.AvatarUrl = profile.AvatarUrl;
            document.UpdatedAt = now;
            document.LastSeenAt = now;

            return document;
        }
        else
        {
            return new CosmosUserDocument
            {
                Id = profile.UserId,
                UserId = profile.UserId,
                Type = "user",
                SchemaVersion = 1,
                Provider = profile.Provider,
                ProviderUserId = profile.ProviderUserId,
                GithubLogin = profile.GithubLogin,
                DisplayName = profile.DisplayName,
                AvatarUrl = profile.AvatarUrl,
                AccessLevel = UserAccessLevels.Guest,
                CreatedAt = now,
                UpdatedAt = now,
                IsDisabled = false,
                FirstSeenAt = now,
                LastSeenAt = now,
                AccessUpdatedAt = null,
                AccessUpdatedBy = null,
            };
        }
    }

    public async Task<UserAccessSnapshot?> GetAccessByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userDocument = await GetByUserIdAsync(
            userId,
            cancellationToken);

        if (userDocument is null)
        {
            return null;
        }

        return new UserAccessSnapshot(
            UserId: userDocument.UserId,
            AccessLevel: userDocument.AccessLevel,
            IsDisabled: userDocument.IsDisabled);
    }
}