using System.Text.Json.Serialization;

using FluentValidation;

using InterviewSimulator.Api.Features.Interviews.Ai;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed record SessionSummaryRequest(
    IReadOnlyList<SessionSummaryTurn> Turns,
    string TargetRole,
    SeniorityLevel Seniority,
    InterviewType InterviewType,
    string FocusArea);

public sealed record SessionSummaryTurn(
    int TurnNumber,
    string QuestionText,
    string QuestionTopic,
    string AnswerText,
    int OverallScore,
    string Feedback,
    IReadOnlyList<SessionSummaryDimension> Dimensions);

public sealed record SessionSummaryDimension(
    string Key,
    int Score,
    string Feedback);

public sealed record SessionSummaryResult(
    string Summary,
    AiCallMetadata AiMetadata);

public sealed record SessionSummaryResponse(
    [property: JsonPropertyName("summary")] string? Summary);

public sealed class SessionSummaryResponseValidator : AbstractValidator<SessionSummaryResponse>
{
    public SessionSummaryResponseValidator(IOptions<AiOptions> options)
    {
        var maxLength = options.Value.MaxSummaryChars;
        RuleFor(x => x.Summary)
            .NotEmpty()
            .WithMessage("summary is required.")
            .MaximumLength(maxLength)
            .WithMessage($"summary must be at most {maxLength} characters long.");
    }
}
