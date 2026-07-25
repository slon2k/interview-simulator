namespace InterviewSimulator.Api.Features.Users;

public sealed record AuthenticatedUserProfile(
    string UserId,
    string Provider,
    string? ProviderUserId,
    string? GithubLogin,
    string? DisplayName,
    string? AvatarUrl);