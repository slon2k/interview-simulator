using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api;

public sealed class AzureSpeechOptions
{
    public const string SectionName = "AzureSpeech";

    public string Region { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;

    public string TokenEndpoint { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;
}

public sealed class AzureSpeechOptionsValidator : IValidateOptions<AzureSpeechOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureSpeechOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            failures.Add("AzureSpeech:Region is required.");
        }

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _))
        {
            failures.Add("AzureSpeech:Endpoint must be an absolute URI.");
        }

        if (!Uri.TryCreate(options.TokenEndpoint, UriKind.Absolute, out _))
        {
            failures.Add("AzureSpeech:TokenEndpoint must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            failures.Add("AzureSpeech:Key is required.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}