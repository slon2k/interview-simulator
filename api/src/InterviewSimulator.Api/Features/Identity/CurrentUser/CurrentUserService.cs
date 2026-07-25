using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity.Access;

namespace InterviewSimulator.Api.Features.Identity.CurrentUser;

public sealed class CurrentUserService(
    IAccessControlService accessControlService) : ICurrentUserService
{
    public async Task<CurrentUser> GetCurrentUserAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var accessStatus = await accessControlService.GetStatus(
            user,
            cancellationToken);

        if (!accessStatus.IsAuthenticated)
        {
            return CurrentUser.Anonymous;
        }

        var identityProvider = user.FindFirstValue(AppClaimTypes.IdentityProvider);
        var displayName = user.FindFirstValue(ClaimTypes.Name);
        var githubLogin = user.FindFirstValue(AppClaimTypes.GitHubLogin);
        var avatarUrl = user.FindFirstValue(AppClaimTypes.GitHubAvatarUrl);

        return new CurrentUser(
            IsAuthenticated: true,
            IsInvited: accessStatus.IsInvited,
            IsAdmin: accessStatus.IsAdmin,
            UserId: accessStatus.UserId,
            IdentityProvider: identityProvider,
            DisplayName: displayName,
            GithubLogin: githubLogin,
            AvatarUrl: avatarUrl);
    }
}