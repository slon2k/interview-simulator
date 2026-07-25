using System.Security.Claims;

namespace InterviewSimulator.Api.Features.Identity.CurrentUser;

public interface ICurrentUserService
{
    Task<CurrentUser> GetCurrentUserAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}

public sealed record CurrentUser(
    bool IsAuthenticated,
    bool IsInvited,
    bool IsAdmin,
    string? UserId,
    string? IdentityProvider,
    string? DisplayName,
    string? GithubLogin,
    string? AvatarUrl)
{
    public static CurrentUser Anonymous { get; } = new(
        IsAuthenticated: false,
        IsInvited: false,
        IsAdmin: false,
        UserId: null,
        IdentityProvider: null,
        DisplayName: null,
        GithubLogin: null,
        AvatarUrl: null);
};