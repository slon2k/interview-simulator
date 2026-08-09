using System.Text.Json.Serialization;

using FluentValidation;

using InterviewSimulator.Api.Features.Interviews.Ai;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed record QuestionGenerationResponse(
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("topic")] string? Topic)
{
    public const int TopicMaxLength = 100;
}

public sealed class QuestionGenerationResponseValidator : AbstractValidator<QuestionGenerationResponse>
{
    public QuestionGenerationResponseValidator(IOptions<AiOptions> options)
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("text is required.")
            .MaximumLength(options.Value.MaxQuestionChars)
            .WithMessage($"text must be at most {options.Value.MaxQuestionChars} characters long.");

        RuleFor(x => x.Topic)
            .NotEmpty()
            .WithMessage("topic is required.")
            .MaximumLength(QuestionGenerationResponse.TopicMaxLength)
            .WithMessage($"topic must be at most {QuestionGenerationResponse.TopicMaxLength} characters long.");
    }
}