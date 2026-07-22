using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.IntegrationTests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task HealthEndpoint_WithValidRoute_ReturnsOk()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownEndpoint_WithMissingRoute_ReturnsProblemDetailsWithTraceId()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(body));

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);

        if (root.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.Number)
        {
            Assert.Equal((int)HttpStatusCode.NotFound, statusElement.GetInt32());
        }

        if (root.TryGetProperty("title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String)
        {
            Assert.False(string.IsNullOrWhiteSpace(titleElement.GetString()));
        }

        if (root.TryGetProperty("traceId", out var traceIdElement) && traceIdElement.ValueKind == JsonValueKind.String)
        {
            Assert.False(string.IsNullOrWhiteSpace(traceIdElement.GetString()));
        }
    }

    [Fact]
    public async Task OpenApi_InDevelopment_ReturnsOk()
    {
        await using var factory = new DevelopmentWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Startup_WithMissingSpeechConfig_ThrowsSanitizedValidationError()
    {
        using var factory = new MissingSpeechConfigWebApplicationFactory();

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("AzureSpeech:Region is required.", exception.Failures);
        Assert.Contains("AzureSpeech:Endpoint must be an absolute URI.", exception.Failures);
        Assert.Contains("AzureSpeech:TokenEndpoint must be an absolute URI.", exception.Failures);
        Assert.Contains("AzureSpeech:Key is required.", exception.Failures);
        Assert.DoesNotContain(exception.Failures, failure => failure.Contains("test-key", StringComparison.Ordinal));
    }

    [Fact]
    public void Startup_WithMissingOpenAIConfig_ThrowsValidationError()
    {
        using var factory = new MissingOpenAIConfigWebApplicationFactory();

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("AzureOpenAI:Endpoint must be an absolute URI.", exception.Failures);
        Assert.Contains("AzureOpenAI:Either DefaultDeploymentName or DeploymentNames[] is required.", exception.Failures);
    }

    private class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Production);

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Cookie:Name"] = "InterviewSimulator.Auth.Test",
                    ["Authentication:GitHub:ClientId"] = "test-client-id",
                    ["Authentication:GitHub:ClientSecret"] = "test-client-secret",
                    [$"{AzureSpeechOptions.SectionName}:Region"] = "centralus",
                    [$"{AzureSpeechOptions.SectionName}:Endpoint"] = "https://example.cognitiveservices.azure.com/",
                    [$"{AzureSpeechOptions.SectionName}:TokenEndpoint"] = "https://centralus.api.cognitive.microsoft.com/sts/v1.0/issueToken",
                    [$"{AzureSpeechOptions.SectionName}:Key"] = "test-key",
                    [$"{AzureOpenAIOptions.SectionName}:Endpoint"] = "https://example.openai.azure.com/",
                    [$"{AzureOpenAIOptions.SectionName}:DefaultDeploymentName"] = "gpt-4o-mini",
                    [$"{AzureOpenAIOptions.SectionName}:DeploymentNames:0"] = "gpt-4o-mini"
                });
            });
        }
    }

    private sealed class DevelopmentWebApplicationFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(Environments.Development);
        }
    }

    private sealed class MissingSpeechConfigWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Production);

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Cookie:Name"] = "InterviewSimulator.Auth.Test",
                    ["Authentication:GitHub:ClientId"] = "test-client-id",
                    ["Authentication:GitHub:ClientSecret"] = "test-client-secret",
                    [$"{AzureSpeechOptions.SectionName}:Region"] = "",
                    [$"{AzureSpeechOptions.SectionName}:Endpoint"] = "",
                    [$"{AzureSpeechOptions.SectionName}:TokenEndpoint"] = "",
                    [$"{AzureSpeechOptions.SectionName}:Key"] = "",
                    [$"{AzureOpenAIOptions.SectionName}:Endpoint"] = "https://example.openai.azure.com/",
                    [$"{AzureOpenAIOptions.SectionName}:DefaultDeploymentName"] = "gpt-4o-mini",
                    [$"{AzureOpenAIOptions.SectionName}:DeploymentNames:0"] = "gpt-4o-mini"
                });
            });
        }
    }

    private sealed class MissingOpenAIConfigWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Production);

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Cookie:Name"] = "InterviewSimulator.Auth.Test",
                    ["Authentication:GitHub:ClientId"] = "test-client-id",
                    ["Authentication:GitHub:ClientSecret"] = "test-client-secret",
                    [$"{AzureSpeechOptions.SectionName}:Region"] = "centralus",
                    [$"{AzureSpeechOptions.SectionName}:Endpoint"] = "https://example.cognitiveservices.azure.com/",
                    [$"{AzureSpeechOptions.SectionName}:TokenEndpoint"] = "https://centralus.api.cognitive.microsoft.com/sts/v1.0/issueToken",
                    [$"{AzureSpeechOptions.SectionName}:Key"] = "test-key",
                    [$"{AzureOpenAIOptions.SectionName}:Endpoint"] = "",
                    [$"{AzureOpenAIOptions.SectionName}:DefaultDeploymentName"] = ""
                });
            });
        }
    }
}
