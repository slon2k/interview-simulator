using System.Security.Claims;
using System.Text.Encodings.Web;

using InterviewSimulator.Api.Features.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.IntegrationTests;

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserIdHeaderName = "X-Test-UserId";
    public const string LoginHeaderName = "X-Test-GitHub-Login";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeaderName, out var userIdValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = userIdValues.ToString();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var login = Request.Headers.TryGetValue(LoginHeaderName, out var loginValues)
            ? loginValues.ToString()
            : "test-user";

        var githubNumericId = userId.StartsWith("github|", StringComparison.Ordinal)
            ? userId["github|".Length..]
            : "0";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, login),

            new(AppClaimTypes.UserId, userId),
            new(AppClaimTypes.IdentityProvider, "github"),
            new(AppClaimTypes.GitHubUserId, githubNumericId),
            new(AppClaimTypes.GitHubLogin, login),
            new(AppClaimTypes.GitHubAvatarUrl, $"https://example.test/avatar/{githubNumericId}.png")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}