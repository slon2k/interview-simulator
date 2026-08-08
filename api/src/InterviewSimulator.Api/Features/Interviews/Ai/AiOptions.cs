namespace InterviewSimulator.Api.Features.Interviews.Ai;

// MaxQuestionGenerationPreviousTurns (default 3) — prior turns included in question prompt
// MaxEvaluationPreviousTurns (default 2) — prior turns included in evaluation prompt
// MaxQuestionChars (default 800) — truncation cap per prior question text
// MaxAnswerChars (default 1200) — truncation cap per prior answer text
// MaxFeedbackChars (default 500) — truncation cap per prior feedback text
// TransientRetryCount (default 1)
// InvalidOutputRetryCount (default 1)

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public int MaxQuestionGenerationPreviousTurns { get; set; } = 3;
    public int MaxEvaluationPreviousTurns { get; set; } = 2;
    public int MaxQuestionChars { get; set; } = 800;
    public int MaxAnswerChars { get; set; } = 1200;
    public int MaxFeedbackChars { get; set; } = 500;
    public int TransientRetryCount { get; set; } = 1;
    public int InvalidOutputRetryCount { get; set; } = 1;
}
