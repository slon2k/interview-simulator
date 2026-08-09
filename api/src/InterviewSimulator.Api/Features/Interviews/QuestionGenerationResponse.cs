using System.Text.Json.Serialization;

using FluentValidation;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed record QuestionGenerationResponse(
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("topic")] string? Topic)
{
    public const int TextMaxLength = 1200;
    public const int TopicMaxLength = 100;
};

public sealed class QuestionGenerationResponseValidator : AbstractValidator<QuestionGenerationResponse>
{
    public QuestionGenerationResponseValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("text is required.")
            .MaximumLength(QuestionGenerationResponse.TextMaxLength)
            .WithMessage($"text must be at most {QuestionGenerationResponse.TextMaxLength} characters long.");

        RuleFor(x => x.Topic)
            .NotEmpty()
            .WithMessage("topic is required.")
            .MaximumLength(QuestionGenerationResponse.TopicMaxLength)
            .WithMessage($"topic must be at most {QuestionGenerationResponse.TopicMaxLength} characters long.");
    }
}