namespace InterviewSimulator.Api.Features.Identity.Profile;

public sealed record AuthenticatedUserProfile(
    string UserId,
    string Provider,
    string? ProviderUserId,
    string? GithubLogin,
    string? DisplayName,
    string? AvatarUrl);