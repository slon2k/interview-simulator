using InterviewSimulator.Api.Options;

namespace InterviewSimulator.Api.Features.Interviews.Ai;

/// <summary>
/// Provider plumbing shared by the Azure OpenAI-backed generator, evaluator, and summarizer.
/// </summary>
public static class AzureOpenAIProvider
{
    public static bool IsTransient(int statusCode) =>
        statusCode is 408 or 429 or 500 or 502 or 503 or 504;

    public static string ResolveDeploymentName(AzureOpenAIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.DefaultDeploymentName))
        {
            return options.DefaultDeploymentName;
        }

        var fallback = options.DeploymentNames.FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        throw new InvalidOperationException("Azure OpenAI deployment is not configured.");
    }
}
