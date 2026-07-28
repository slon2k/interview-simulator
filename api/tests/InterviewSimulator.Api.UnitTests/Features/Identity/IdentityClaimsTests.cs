using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.UnitTests.Features.Identity;

public class IdentityClaims_GetUserId
{
    [Fact]
    public void PrefersAppUserIdClaim()
    {
        var user = BuildPrincipal(
            (AppClaimTypes.UserId, "github|100"),
            (ClaimTypes.NameIdentifier, "name-id-fallback"));

        Assert.Equal("github|100", IdentityClaims.GetUserId(user));
    }

    [Fact]
    public void FallsBackToNameIdentifier()
    {
        var user = BuildPrincipal((ClaimTypes.NameIdentifier, "name-id-fallback"));

        Assert.Equal("name-id-fallback", IdentityClaims.GetUserId(user));
    }

    [Fact]
    public void ReturnsNullWhenNoIdentifyingClaim()
    {
        var user = BuildPrincipal((ClaimTypes.Name, "Ada Lovelace"));

        Assert.Null(IdentityClaims.GetUserId(user));
    }

    internal static ClaimsPrincipal BuildPrincipal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Type, claim.Value)),
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}

public class IdentityClaims_ToAuthenticatedUserProfile
{
    [Fact]
    public void MapsAllClaimsToProfile()
    {
        var user = IdentityClaims_GetUserId.BuildPrincipal(
            (AppClaimTypes.UserId, "github|100"),
            (AppClaimTypes.IdentityProvider, "github"),
            (AppClaimTypes.GitHubUserId, "100"),
            (AppClaimTypes.GitHubLogin, "ada"),
            (ClaimTypes.Name, "Ada Lovelace"),
            (AppClaimTypes.GitHubAvatarUrl, "https://example.test/ada.png"));

        var profile = IdentityClaims.ToAuthenticatedUserProfile(user);

        Assert.NotNull(profile);
        Assert.Equal("github|100", profile.UserId);
        Assert.Equal("github", profile.Provider);
        Assert.Equal("100", profile.ProviderUserId);
        Assert.Equal("ada", profile.GithubLogin);
        Assert.Equal("Ada Lovelace", profile.DisplayName);
        Assert.Equal("https://example.test/ada.png", profile.AvatarUrl);
    }

    [Fact]
    public void DefaultsProviderToUnknownWhenClaimMissing()
    {
        var user = IdentityClaims_GetUserId.BuildPrincipal((AppClaimTypes.UserId, "github|100"));

        var profile = IdentityClaims.ToAuthenticatedUserProfile(user);

        Assert.NotNull(profile);
        Assert.Equal("unknown", profile.Provider);
        Assert.Null(profile.ProviderUserId);
        Assert.Null(profile.GithubLogin);
        Assert.Null(profile.DisplayName);
        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public void ReturnsNullWhenUserIdCannotBeResolved()
    {
        var user = IdentityClaims_GetUserId.BuildPrincipal((AppClaimTypes.GitHubLogin, "ada"));

        Assert.Null(IdentityClaims.ToAuthenticatedUserProfile(user));
    }
}
