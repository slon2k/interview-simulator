using System.Globalization;

namespace InterviewSimulator.Api.Features.Interviews;

public sealed class HardcodedQuestionGenerator : IQuestionGenerator
{
    private static readonly string[] TechnicalTemplates =
    [
        "For a {0} {1} role, how would you approach solving a core {2} problem from first principles?",
        "Describe a {2} implementation you have built or would build for a {0} {1} position, including key trade-offs.",
        "If this {2} feature failed in production, how would you diagnose and stabilize it as a {0} {1}?"
    ];

    private static readonly string[] BehavioralTemplates =
    [
        "Tell me about a time you handled conflict while delivering {2} outcomes in a {0} {1} role.",
        "Describe how you prioritize work and communication under pressure for {2} responsibilities as a {0} {1}.",
        "Share an example of feedback you received while working on {2} and how it changed your approach as a {0} {1}."
    ];

    private static readonly string[] SystemDesignTemplates =
    [
        "Design a high-level architecture for a {2} system suitable for a {0} {1} role.",
        "For a {2} platform, explain how you would design scalability, reliability, and observability as a {0} {1}.",
        "Walk through the data flow and failure handling you would choose for a {2} service in a {0} {1} position."
    ];

    public Task<GeneratedQuestion> GenerateQuestionAsync(
        GenerateQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.PreviousTurns);

        cancellationToken.ThrowIfCancellationRequested();

        var targetRole = RequireNonEmpty(request.TargetRole, nameof(request.TargetRole));
        var focusArea = RequireNonEmpty(request.FocusArea, nameof(request.FocusArea));

        if (request.TurnNumber <= 0)
        {
            throw new ArgumentException("Turn number must be greater than zero.", nameof(request));
        }

        if (request.QuestionCount <= 0)
        {
            throw new ArgumentException("Question count must be greater than zero.", nameof(request));
        }

        if (request.TurnNumber > request.QuestionCount)
        {
            throw new ArgumentException("Turn number cannot exceed question count.", nameof(request));
        }

        var templates = GetTemplates(request.InterviewType);
        var templateIndex = (request.TurnNumber - 1) % templates.Length;

        var seniorityLabel = GetSeniorityLabel(request.Seniority);

        var coreQuestion = string.Format(
            CultureInfo.InvariantCulture,
            templates[templateIndex],
            seniorityLabel,
            targetRole,
            focusArea);

        var followUpPrefix = BuildFollowUpPrefix(request);
        var questionText = string.Concat(followUpPrefix, coreQuestion);

        return Task.FromResult(new GeneratedQuestion(
            Text: questionText,
            Topic: focusArea));
    }

    private static string[] GetTemplates(InterviewType interviewType)
    {
        return interviewType switch
        {
            InterviewType.Technical => TechnicalTemplates,
            InterviewType.Behavioral => BehavioralTemplates,
            InterviewType.SystemDesign => SystemDesignTemplates,
            _ => throw new ArgumentOutOfRangeException(
                nameof(interviewType),
                interviewType,
                "Unsupported interview type.")
        };
    }

    private static string GetSeniorityLabel(SeniorityLevel seniority)
    {
        return seniority switch
        {
            SeniorityLevel.Junior => "junior",
            SeniorityLevel.Middle => "mid-level",
            SeniorityLevel.Senior => "senior",
            _ => throw new ArgumentOutOfRangeException(
                nameof(seniority),
                seniority,
                "Unsupported seniority level.")
        };
    }

    private static string BuildFollowUpPrefix(GenerateQuestionRequest request)
    {
        if (request.TurnNumber <= 1 || request.PreviousTurns.Count == 0)
        {
            return string.Empty;
        }

        var previousTopic = request.PreviousTurns[^1].QuestionTopic;

        if (string.IsNullOrWhiteSpace(previousTopic))
        {
            return "Building on your previous answer: ";
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "Building on your previous answer about {0}: ",
            previousTopic.Trim());
    }

    private static string RequireNonEmpty(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
