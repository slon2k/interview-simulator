using System.Security.Claims;

using AspNet.Security.OAuth.GitHub;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace InterviewSimulator.Api.Features.Auth;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/auth/login", LoginHandler)
            .AllowAnonymous();

        endpoints.MapPost("/api/auth/logout", LogoutHandler)
            .AllowAnonymous();

        endpoints.MapGet("/api/me", MeHandler)
            .AllowAnonymous();

        endpoints.MapGet("/api/auth/smoke", SmokeHandler)
            .RequireAuthorization();

        return endpoints;
    }

    private static IResult LogoutHandler()
    {
        return Results.SignOut(
            authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme]);
    }

    private static IResult LoginHandler(string? returnUrl)
    {
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);

        var properties = new AuthenticationProperties
        {
            RedirectUri = safeReturnUrl
        };

        return Results.Challenge(
            properties,
            authenticationSchemes: [GitHubAuthenticationDefaults.AuthenticationScheme]);
    }

    private static IResult MeHandler(ClaimsPrincipal user)
    {
        var isAuthenticated = user.Identity?.IsAuthenticated == true;

        if (!isAuthenticated)
        {
            return Results.Ok(new CurrentUserResponse(
                IsAuthenticated: false,
                IsInvited: false,
                IsAdmin: false,
                UserId: null,
                IdentityProvider: null,
                DisplayName: null,
                GithubLogin: null,
                AvatarUrl: null));
        }

        var userId =
            user.FindFirstValue(AppClaimTypes.UserId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        var identityProvider = user.FindFirstValue(AppClaimTypes.IdentityProvider);
        var displayName = user.FindFirstValue(ClaimTypes.Name);
        var githubLogin = user.FindFirstValue(AppClaimTypes.GitHubLogin);
        var avatarUrl = user.FindFirstValue(AppClaimTypes.GitHubAvatarUrl);

        return Results.Ok(new CurrentUserResponse(
            IsAuthenticated: true,

            // These will become real in 02b.
            IsInvited: false,
            IsAdmin: false,

            UserId: userId,
            IdentityProvider: identityProvider,
            DisplayName: displayName,
            GithubLogin: githubLogin,
            AvatarUrl: avatarUrl));
    }

    private static IResult SmokeHandler(ClaimsPrincipal user)
    {
        var userId =
            user.FindFirstValue(AppClaimTypes.UserId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Results.Ok(new
        {
            status = "authenticated",
            userId
        });
    }

    private static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (!returnUrl.StartsWith('/'))
        {
            return "/";
        }

        if (returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        if (returnUrl.Contains('\\', StringComparison.Ordinal))
        {
            return "/";
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Relative, out _))
        {
            return "/";
        }

        return returnUrl;
    }
}