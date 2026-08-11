namespace InterviewSimulator.Api.Features.Interviews.Ai;

public static class EvaluationRubrics
{
    public static EvaluationRubric GetForInterviewType(InterviewType interviewType) => interviewType switch
    {
        InterviewType.Technical => new EvaluationRubric(
            interviewType,
            PromptVersions.EvaluationTechnical,
            [
                new("technicalCorrectness", "Technical Correctness", "Technical accuracy and whether the answer addresses the question."),
                new("depth", "Depth", "Depth of understanding, details, edge cases, and trade-offs."),
                new("communication", "Communication", "Clarity, structure, and ease of understanding."),
                new("problemSolving", "Problem Solving", "Reasoning process, approach, and adaptation to constraints.")
            ]),
        InterviewType.Behavioral => new EvaluationRubric(
            interviewType,
            PromptVersions.EvaluationBehavioral,
            [
                new("situationContext", "Situation & Context", "Clarity of the situation, background, and the problem being faced."),
                new("actionTaken", "Action Taken", "Specific actions taken, ownership, and decision-making process."),
                new("result", "Result", "Outcome achieved, impact, and measurable results."),
                new("reflection", "Reflection", "Lessons learned, self-awareness, and growth from the experience.")
            ]),
        InterviewType.SystemDesign => new EvaluationRubric(
            interviewType,
            PromptVersions.EvaluationSystemDesign,
            [
                new("requirementsClarity", "Requirements Clarity", "Requirements, constraints, assumptions, and clarifying questions."),
                new("componentDesign", "Component Design", "Architecture, components, APIs, data models, and interactions."),
                new("scalability", "Scalability", "Scale, performance, reliability, availability, and bottlenecks."),
                new("tradeoffs", "Trade-offs", "Alternatives, limitations, operational concerns, and design trade-offs.")
            ]),
        _ => throw new ArgumentOutOfRangeException(nameof(interviewType), $"Unsupported interview type: {interviewType}")
    };
}

public sealed record EvaluationRubric(
    InterviewType InterviewType,
    string PromptVersion,
    IReadOnlyList<EvaluationRubricDimension> Dimensions);

public sealed record EvaluationRubricDimension(
    string Key,
    string Label,
    string Description);