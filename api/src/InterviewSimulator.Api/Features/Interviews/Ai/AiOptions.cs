namespace InterviewSimulator.Api.Features.Interviews.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public const int PreviousTurnsLimit = 100;

    public string Provider { get; set; } = AiProviders.AzureOpenAI;
    public int MaxQuestionGenerationPreviousTurns { get; set; } = 3;
    public int MaxEvaluationPreviousTurns { get; set; } = 2;
    public int MaxQuestionChars { get; set; } = 400;
    public int MaxAnswerChars { get; set; } = 2400;
    public int MaxFeedbackChars { get; set; } = 1200;

    public int MaxSummaryChars { get; set; } = 2400;
    public int TransientRetryCount { get; set; } = 1;
    public int InvalidOutputRetryCount { get; set; } = 1;
}
