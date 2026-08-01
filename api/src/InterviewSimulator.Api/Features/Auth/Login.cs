using AspNet.Security.OAuth.GitHub;

using Microsoft.AspNetCore.Authentication;

namespace InterviewSimulator.Api.Features.Auth;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder app)
    {
        app.MapGet("/login", Handler)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Initiate GitHub OAuth login")
            .WithDescription("Redirects the user to GitHub for OAuth authentication. Accepts an optional relative returnUrl to redirect back to after login.");

        return app;
    }

    public static IResult Handler(string? returnUrl)
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
