using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.CurrentUser;
using InterviewSimulator.Api.Features.Identity.Profile;
using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Infrastructure.Data;
using InterviewSimulator.Api.Options;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests.Auth;

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:GitHub:ClientId"] = "test-client-id",
                ["Authentication:GitHub:ClientSecret"] = "test-client-secret",
                ["Authentication:Cookie:Name"] = "InterviewSimulator.Auth.Test",

                // Access-control test data.
                ["AccessControl:AdminUserIds:0"] = "github|200",

                [$"{AzureSpeechOptions.SectionName}:Region"] = "centralus",
                [$"{AzureSpeechOptions.SectionName}:Endpoint"] = "https://example.cognitiveservices.azure.com/",
                [$"{AzureSpeechOptions.SectionName}:TokenEndpoint"] = "https://centralus.api.cognitive.microsoft.com/sts/v1.0/issueToken",
                [$"{AzureSpeechOptions.SectionName}:Key"] = "test-key",
                [$"{AzureOpenAIOptions.SectionName}:Endpoint"] = "https://example.openai.azure.com/",
                [$"{AzureOpenAIOptions.SectionName}:DefaultDeploymentName"] = "gpt-4o-mini",
                [$"{AzureOpenAIOptions.SectionName}:DeploymentNames:0"] = "gpt-4o-mini"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace identity stores with test implementations (Cosmos is disabled, so these are normally no-ops).
            services.AddScoped<IUserProfileStore>(_ => new TestUserProfileStore());
            services.AddScoped<IUserAccessReader>(_ => new TestUserAccessReader());
            services.AddScoped<IAnswerEvaluator, HardcodedAnswerEvaluator>();

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            // Use the test scheme for authentication, but keep the app's cookie scheme
            // for challenge/forbid responses so 401/403 JSON behavior is still tested.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });
        });
    }
}

/// <summary>
/// Test implementation of IUserProfileStore that seeds user profiles for testing.
/// </summary>
file sealed class TestUserProfileStore : IUserProfileStore
{
    public Task UpsertAuthenticatedUserProfileAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        // No-op: profiles are pre-seeded in TestUserAccessReader
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test implementation of IUserAccessReader that returns pre-seeded access levels for testing.
/// github|100 → Member, github|200 → Admin, all others → null (guest/unknown).
/// </summary>
file sealed class TestUserAccessReader : IUserAccessReader
{
    private static readonly Dictionary<string, UserAccessSnapshot> _accessLevels = new()
    {
        ["github|100"] = new UserAccessSnapshot(
            UserId: "github|100",
            AccessLevel: UserAccessLevels.Member,
            IsDisabled: false),
        ["github|200"] = new UserAccessSnapshot(
            UserId: "github|200",
            AccessLevel: UserAccessLevels.Admin,
            IsDisabled: false),
    };

    public Task<UserAccessSnapshot?> GetAccessByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_accessLevels.GetValueOrDefault(userId));
}