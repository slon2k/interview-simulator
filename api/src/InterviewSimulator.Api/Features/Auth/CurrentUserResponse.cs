namespace InterviewSimulator.Api.Features.Auth;

public sealed record CurrentUserResponse(
    bool IsAuthenticated,
    bool IsInvited,
    bool IsAdmin,
    string? UserId,
    string? IdentityProvider,
    string? DisplayName,
    string? GitHubLogin,
    string? AvatarUrl);