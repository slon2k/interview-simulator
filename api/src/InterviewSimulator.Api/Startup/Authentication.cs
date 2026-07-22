using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

using InterviewSimulator.Api.Features.Auth;

using Microsoft.AspNetCore.Authentication.Cookies;

namespace InterviewSimulator.Api.Startup;

public static class Authentication
{
    public static WebApplicationBuilder AddApplicationAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            // Keep default challenge as cookie so protected /api/* endpoints return 401/403, not automatic GitHub redirects.
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            var cookieName = builder.Configuration["Authentication:Cookie:Name"];

            if (string.IsNullOrWhiteSpace(cookieName))
            {
                throw new InvalidOperationException("Missing configuration: Authentication:Cookie:Name");
            }

            options.Cookie.Name = cookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);

            options.Events.OnRedirectToLogin = context =>
            {
                return WriteAuthErrorAsync(
                    context.Response,
                    StatusCodes.Status401Unauthorized,
                    "unauthorized",
                    "Authentication is required.");
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                return WriteAuthErrorAsync(
                    context.Response,
                    StatusCodes.Status403Forbidden,
                    "forbidden",
                    "You do not have access to this resource.");
            };
        })
        .AddGitHub(options =>
        {
            var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
            var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];

            if (string.IsNullOrWhiteSpace(githubClientId))
            {
                throw new InvalidOperationException("Missing configuration: Authentication:GitHub:ClientId");
            }

            if (string.IsNullOrWhiteSpace(githubClientSecret))
            {
                throw new InvalidOperationException("Missing configuration: Authentication:GitHub:ClientSecret");
            }

            options.ClientId = githubClientId;
            options.ClientSecret = githubClientSecret;
            options.CallbackPath = "/signin-github";
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = false;

            options.Scope.Add("read:user");

            options.Events.OnCreatingTicket = context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                {
                    context.Fail("GitHub authentication did not produce an identity.");
                    return Task.CompletedTask;
                }

                if (!context.User.TryGetProperty("id", out var githubIdElement))
                {
                    context.Fail("GitHub user id was not returned.");
                    return Task.CompletedTask;
                }

                var githubUserId = githubIdElement.ValueKind switch
                {
                    JsonValueKind.Number => githubIdElement.GetInt64().ToString(CultureInfo.InvariantCulture),
                    JsonValueKind.String => githubIdElement.GetString(),
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(githubUserId))
                {
                    context.Fail("GitHub user id was empty.");
                    return Task.CompletedTask;
                }

                var login = GetJsonString(context.User, "login");
                var name = GetJsonString(context.User, "name");
                var avatarUrl = GetJsonString(context.User, "avatar_url");

                var displayName = !string.IsNullOrWhiteSpace(name)
                    ? name
                    : login ?? $"GitHub user {githubUserId}";

                var appUserId = $"github|{githubUserId}";

                AddOrReplaceClaim(identity, ClaimTypes.NameIdentifier, appUserId);
                AddOrReplaceClaim(identity, ClaimTypes.Name, displayName);

                AddOrReplaceClaim(identity, AppClaimTypes.UserId, appUserId);
                AddOrReplaceClaim(identity, AppClaimTypes.IdentityProvider, "github");
                AddOrReplaceClaim(identity, AppClaimTypes.GitHubUserId, githubUserId);

                if (!string.IsNullOrWhiteSpace(login))
                {
                    AddOrReplaceClaim(identity, AppClaimTypes.GitHubLogin, login);
                }

                if (!string.IsNullOrWhiteSpace(avatarUrl))
                {
                    AddOrReplaceClaim(identity, AppClaimTypes.GitHubAvatarUrl, avatarUrl);
                }

                return Task.CompletedTask;
            };

            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/login?error=github-oauth-failed");
                return Task.CompletedTask;
            };
        });

        builder.Services.AddAuthorization();

        return builder;
    }

    private static Task WriteAuthErrorAsync(
        HttpResponse response,
        int statusCode,
        string error,
        string message)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";

        return response.WriteAsJsonAsync(new
        {
            error,
            message
        });
    }

    private static void AddOrReplaceClaim(ClaimsIdentity identity, string claimType, string claimValue)
    {
        foreach (var existingClaim in identity.FindAll(claimType).ToArray())
        {
            identity.RemoveClaim(existingClaim);
        }

        identity.AddClaim(new Claim(claimType, claimValue));
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            return null;
        }

        return propertyValue.ValueKind switch
        {
            JsonValueKind.String => propertyValue.GetString(),
            JsonValueKind.Number => propertyValue.GetInt64().ToString(CultureInfo.InvariantCulture),
            _ => null
        };
    }
}