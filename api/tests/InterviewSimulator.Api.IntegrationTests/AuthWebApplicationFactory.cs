using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.IntegrationTests;

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
                ["AccessControl:InvitedUserIds:0"] = "github|100",
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