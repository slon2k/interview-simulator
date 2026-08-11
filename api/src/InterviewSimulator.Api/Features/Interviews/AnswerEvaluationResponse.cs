using System.Text.Json.Serialization;

using FluentValidation;

using InterviewSimulator.Api.Features.Interviews.Ai;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed record AnswerEvaluationResponseDimension(
    [property: JsonPropertyName("key")] string? Key,
    [property: JsonPropertyName("score")] int? Score,
    [property: JsonPropertyName("feedback")] string? Feedback);

public sealed record AnswerEvaluationResponse(
    [property: JsonPropertyName("dimensions")] IReadOnlyList<AnswerEvaluationResponseDimension>? Dimensions,
    [property: JsonPropertyName("feedback")] string? Feedback);

public sealed class AnswerEvaluationResponseValidator : AbstractValidator<AnswerEvaluationResponse>
{
    public AnswerEvaluationResponseValidator(IOptions<AiOptions> options)
    {
        RuleFor(x => x.Feedback)
            .NotEmpty()
            .WithMessage("feedback is required.")
            .MaximumLength(options.Value.MaxFeedbackChars)
            .WithMessage($"feedback must be at most {options.Value.MaxFeedbackChars} characters long.");

        RuleFor(x => x.Dimensions)
            .NotEmpty()
            .WithMessage("dimensions must be a non-empty array.")
            .Must(HaveUniqueKeys)
            .WithMessage("dimension keys must be unique.");

        RuleForEach(x => x.Dimensions).ChildRules(dimension =>
        {
            dimension.RuleFor(d => d.Key)
                .NotEmpty()
                .WithMessage("dimension key is required.");

            dimension.RuleFor(d => d.Score)
                .NotNull()
                .WithMessage("dimension score is required.")
                .InclusiveBetween(0, 100)
                .WithMessage("dimension score must be between 0 and 100.");

            dimension.RuleFor(d => d.Feedback)
                .NotEmpty()
                .WithMessage("dimension feedback is required.");
        });
    }

    private static bool HaveUniqueKeys(IReadOnlyList<AnswerEvaluationResponseDimension>? dimensions)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dimension in dimensions ?? [])
        {
            if (!keys.Add(dimension.Key ?? string.Empty))
            {
                return false;
            }
        }
        return true;
    }

}
