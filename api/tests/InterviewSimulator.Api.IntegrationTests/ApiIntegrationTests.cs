using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace InterviewSimulator.Api.IntegrationTests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task HealthEndpoint_WithValidRoute_ReturnsOk()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownEndpoint_WithMissingRoute_ReturnsProblemDetailsWithTraceId()
    {
        await using var factory = new WebApplicationFactory<Program>();
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

    private sealed class DevelopmentWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            return base.CreateHost(builder);
        }
    }

    private sealed class ProblemDetailsResponse
    {
        public string? Title { get; init; }

        public int? Status { get; init; }

        public string? TraceId { get; init; }
    }
}
