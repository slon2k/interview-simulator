using InterviewSimulator.Api.Features.Interviews.Ai;
using InterviewSimulator.Api.Options;

namespace InterviewSimulator.Api.UnitTests.Features.Interviews.Ai;

public sealed class AzureOpenAIProvider_IsTransient
{
    [Theory]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public void IsTransient_WithRetryableStatus_ReturnsTrue(int statusCode)
    {
        Assert.True(AzureOpenAIProvider.IsTransient(statusCode));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(422)]
    [InlineData(501)]
    public void IsTransient_WithNonRetryableStatus_ReturnsFalse(int statusCode)
    {
        Assert.False(AzureOpenAIProvider.IsTransient(statusCode));
    }
}

public sealed class AzureOpenAIProvider_ResolveDeploymentName
{
    [Fact]
    public void ResolveDeploymentName_WithDefaultConfigured_ReturnsDefault()
    {
        var options = new AzureOpenAIOptions
        {
            DefaultDeploymentName = "gpt-4o-mini",
            DeploymentNames = ["ignored"],
        };

        Assert.Equal("gpt-4o-mini", AzureOpenAIProvider.ResolveDeploymentName(options));
    }

    [Fact]
    public void ResolveDeploymentName_WithoutDefault_FallsBackToFirstNonBlankName()
    {
        var options = new AzureOpenAIOptions
        {
            DefaultDeploymentName = "   ",
            DeploymentNames = ["", "  ", "gpt-4o"],
        };

        Assert.Equal("gpt-4o", AzureOpenAIProvider.ResolveDeploymentName(options));
    }

    [Fact]
    public void ResolveDeploymentName_WithNoDeploymentConfigured_Throws()
    {
        var options = new AzureOpenAIOptions
        {
            DefaultDeploymentName = string.Empty,
            DeploymentNames = ["", "   "],
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AzureOpenAIProvider.ResolveDeploymentName(options));

        Assert.Contains("not configured", ex.Message, StringComparison.Ordinal);
    }
}
