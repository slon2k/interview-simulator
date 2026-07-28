using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity.Profile;

namespace InterviewSimulator.Api.Features.Identity;

/// <summary>
/// Canonical rules for reading identity information off a <see cref="ClaimsPrincipal"/>.
/// Kept in one place so the user-id fallback and profile shape are defined exactly once.
/// </summary>
public static class IdentityClaims
{
    /// <summary>
    /// Resolves the canonical application user ID, falling back to the standard
    /// name-identifier claim when the application claim is absent.
    /// </summary>
    public static string? GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(AppClaimTypes.UserId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Builds an <see cref="AuthenticatedUserProfile"/> from the principal's claims,
    /// or returns null when no user ID can be resolved.
    /// </summary>
    public static AuthenticatedUserProfile? ToAuthenticatedUserProfile(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return new AuthenticatedUserProfile(
            UserId: userId,
            Provider: user.FindFirstValue(AppClaimTypes.IdentityProvider) ?? "unknown",
            ProviderUserId: user.FindFirstValue(AppClaimTypes.GitHubUserId),
            GithubLogin: user.FindFirstValue(AppClaimTypes.GitHubLogin),
            DisplayName: user.FindFirstValue(ClaimTypes.Name),
            AvatarUrl: user.FindFirstValue(AppClaimTypes.GitHubAvatarUrl));
    }
}
