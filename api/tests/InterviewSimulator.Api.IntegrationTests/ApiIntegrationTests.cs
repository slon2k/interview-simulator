using System.Net;
using System.Net.Http.Json;
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

        var payload = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Request failed", payload.Title);
        Assert.Equal((int)HttpStatusCode.NotFound, payload.Status);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
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

    private class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{AzureSpeechOptions.SectionName}:Region"] = "centralus",
                    [$"{AzureSpeechOptions.SectionName}:Endpoint"] = "https://example.cognitiveservices.azure.com/",
                    [$"{AzureSpeechOptions.SectionName}:TokenEndpoint"] = "https://centralus.api.cognitive.microsoft.com/sts/v1.0/issueToken",
                    [$"{AzureSpeechOptions.SectionName}:Key"] = "test-key"
                });
            });
        }
    }

    private sealed class DevelopmentWebApplicationFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            base.ConfigureWebHost(builder);
        }
    }

    private sealed class MissingSpeechConfigWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{AzureSpeechOptions.SectionName}:Region"] = "",
                    [$"{AzureSpeechOptions.SectionName}:Endpoint"] = "",
                    [$"{AzureSpeechOptions.SectionName}:TokenEndpoint"] = "",
                    [$"{AzureSpeechOptions.SectionName}:Key"] = ""
                });
            });
        }
    }

    private sealed class ProblemDetailsResponse
    {
        public string? Title { get; init; }

        public int? Status { get; init; }

        public string? TraceId { get; init; }
    }
}
