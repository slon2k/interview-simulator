using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api;

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; init; } = string.Empty;

    public string DefaultDeploymentName { get; init; } = string.Empty;

    public string[] DeploymentNames { get; init; } = [];
}

public sealed class AzureOpenAIOptionsValidator : IValidateOptions<AzureOpenAIOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureOpenAIOptions options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _))
        {
            failures.Add("AzureOpenAI:Endpoint must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(options.DefaultDeploymentName))
        {
            var hasAnyDeploymentName = options.DeploymentNames.Any(name => !string.IsNullOrWhiteSpace(name));

            if (!hasAnyDeploymentName)
            {
                failures.Add("AzureOpenAI:Either DefaultDeploymentName or DeploymentNames[] is required.");
            }
        }

        if (options.DeploymentNames.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("AzureOpenAI:DeploymentNames[] cannot contain empty values.");
        }

        if (options.DeploymentNames.Length > 0
            && !string.IsNullOrWhiteSpace(options.DefaultDeploymentName)
            && !options.DeploymentNames.Contains(options.DefaultDeploymentName, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add("AzureOpenAI:DefaultDeploymentName must exist in DeploymentNames[] when both are provided.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}