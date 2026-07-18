using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api;

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; init; } = string.Empty;

    public string DeploymentName { get; init; } = string.Empty;
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

        if (string.IsNullOrWhiteSpace(options.DeploymentName))
        {
            failures.Add("AzureOpenAI:DeploymentName is required.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}