using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.Profile;
using InterviewSimulator.Api.Infrastructure.Data;

namespace InterviewSimulator.Api.Infrastructure.Identity;

public static class UserDocumentMapper
{
    /// <summary>
    /// Applies the profile to an existing document, or creates a new one when the user is unknown.
    /// Access level and <see cref="CosmosUserDocument.FirstSeenAt"/> are never overwritten for existing users.
    /// </summary>
    public static CosmosUserDocument CreateOrUpdate(
        CosmosUserDocument? existing,
        AuthenticatedUserProfile profile,
        DateTimeOffset now)
    {
        if (existing is not null)
        {
            existing.Provider = profile.Provider;
            existing.ProviderUserId = profile.ProviderUserId;
            existing.GithubLogin = profile.GithubLogin;
            existing.DisplayName = profile.DisplayName;
            existing.AvatarUrl = profile.AvatarUrl;
            existing.UpdatedAt = now;
            existing.LastSeenAt = now;

            return existing;
        }

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

    public static UserAccessSnapshot ToAccessSnapshot(CosmosUserDocument document)
        => new(
            UserId: document.UserId,
            AccessLevel: document.AccessLevel,
            IsDisabled: document.IsDisabled);
}
