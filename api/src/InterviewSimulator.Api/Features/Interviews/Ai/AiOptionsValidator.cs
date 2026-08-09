using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Interviews.Ai;

public sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            failures.Add("Ai:Provider is required.");
        }
        else if (!string.Equals(options.Provider, AiProviders.AzureOpenAI, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(options.Provider, AiProviders.Hardcoded, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"Ai:Provider must be one of: {AiProviders.AzureOpenAI}, {AiProviders.Hardcoded}.");
        }

        if (options.MaxQuestionGenerationPreviousTurns < 0)
        {
            failures.Add("Ai:MaxQuestionGenerationPreviousTurns must be greater than or equal to 0.");
        }

        if (options.MaxQuestionGenerationPreviousTurns > AiOptions.PreviousTurnsLimit)
        {
            failures.Add($"Ai:MaxQuestionGenerationPreviousTurns must be less than or equal to {AiOptions.PreviousTurnsLimit}.");
        }

        if (options.MaxEvaluationPreviousTurns < 0)
        {
            failures.Add("Ai:MaxEvaluationPreviousTurns must be greater than or equal to 0.");
        }

        if (options.MaxQuestionChars <= 0)
        {
            failures.Add("Ai:MaxQuestionChars must be greater than 0.");
        }

        if (options.MaxAnswerChars <= 0)
        {
            failures.Add("Ai:MaxAnswerChars must be greater than 0.");
        }

        if (options.MaxFeedbackChars <= 0)
        {
            failures.Add("Ai:MaxFeedbackChars must be greater than 0.");
        }

        if (options.TransientRetryCount < 0)
        {
            failures.Add("Ai:TransientRetryCount must be greater than or equal to 0.");
        }

        if (options.InvalidOutputRetryCount < 0)
        {
            failures.Add("Ai:InvalidOutputRetryCount must be greater than or equal to 0.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}