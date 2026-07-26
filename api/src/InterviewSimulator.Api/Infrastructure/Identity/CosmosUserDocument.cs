using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Infrastructure.Data;

namespace InterviewSimulator.Api.Infrastructure.Identity;

/// <summary>
/// Represents a user document stored in the system, containing information about the user's identity, access level, and timestamps for tracking user activity.
/// </summary>
public sealed class CosmosUserDocument : ICosmosDocument, IUserCosmosDocument
{
    public string Id { get; init; } = string.Empty;

    public string Type { get; set; } = "user";

    public int SchemaVersion { get; set; } = 1;

    public string UserId { get; init; } = string.Empty;

    public string Provider { get; set; } = "github";

    public string? ProviderUserId { get; set; }

    public string? GithubLogin { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public string AccessLevel { get; set; } = UserAccessLevels.Guest;

    public bool IsDisabled { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? AccessUpdatedAt { get; set; }

    public string? AccessUpdatedBy { get; set; }
}