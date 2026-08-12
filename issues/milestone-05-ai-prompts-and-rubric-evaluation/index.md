# Milestone 05 - AI prompts and rubric evaluation

Epic type: Milestone

## Overview

Replace the M04 hardcoded stubs with configurable Azure OpenAI-backed question generation and rubric-based per-answer evaluation. Add an AI boundary with prompt versioning, bounded context windows, structured response validation, conservative retries, and graceful degradation.

M04 proved the full session lifecycle works end-to-end with deterministic stubs behind `IQuestionGenerator`. M05 swaps in real generation, introduces `IAnswerEvaluator` for per-answer structured feedback, and hardens the AI boundary so that AI failures degrade to 503 and never corrupt interview state.

**M05 uses synchronous AI calls.** AI generation and evaluation complete before Cosmos persistence. If any AI operation fails, no interview state is modified and the user may retry.

The endpoint set remains unchanged. The interview response contract remains minimal in M05; per-turn evaluation payloads are deferred to M06.

## Feature Issues

- 05a - AI boundary foundation (`AiStructuredOutputRunner`, `PromptVersions`, `AiOptions`, typed exceptions, 503 error routing)
- 05b - Azure OpenAI question generation (real `IQuestionGenerator`, config-driven selection, generation metadata on turns)
- 05c - Rubric-based answer evaluation (`IAnswerEvaluator`, rubrics, domain model expansion, persistence, contract update)
- 05d - End-to-end AI flow verification and documentation

## Key Decisions

- **Reuse M04 abstractions**: `IQuestionGenerator` gets a real Azure OpenAI implementation; `HardcodedQuestionGenerator` is retained for CI and offline runs. Selection is config-driven via `Ai:Provider`.
- **New abstraction for evaluation**: `IAnswerEvaluator` is a separate, single-purpose interface. Question generation and answer scoring are distinct capabilities and must not be merged into one AI service.
- **Synchronous AI before persistence**: AI generation and evaluation complete before `SaveAnswerAsync` is called. A failed AI call returns a 503 and leaves the session in a resumable state. No partial turn writes.
- **Structured output**: evaluation returns a typed result validated against a rubric-defined schema. AI is instructed to return JSON with per-dimension scores; the app validates and rejects or retries malformed responses.
- **App-computed overall score**: AI returns per-dimension scores. `OverallScore` is computed by the app as the rounded integer average of dimension scores. AI does not return an overall score.
- **Rubrics per interview type**: Technical, Behavioral, and SystemDesign each have their own rubric with stable dimension keys. The interview type on the session selects the rubric.
- **Prompt versioning in code**: prompt versions are string constants in `PromptVersions.cs` following the `{purpose}-v{N}` convention. Bumping N when prompt text changes. Not configurable at runtime.
- **Separate AI metadata per operation**: question generation metadata (`questionAi`) and evaluation metadata (`evaluationAi`) are stored as separate fields on `CosmosTurnDocument`. A single flat field would be ambiguous once a turn has both.
- **503 for AI failures**: all typed AI exceptions map to 503 ProblemDetails. Azure-specific exceptions are wrapped inside AI adapters and never reach the global handler.
- **Conservative retry**: one retry on malformed or invalid AI output; one retry on transient provider failure. Auth and config failures are not retried.
- **`lastEvaluation` vs `feedback`**: `lastEvaluation` on `InterviewResponse` carries the latest per-answer rubric result. The existing `feedback` field (`FeedbackContract`) is reserved for M06 session-level summaries and remains null in M05.

## Interface Shape

Generation reuses the M04 interface. `GeneratedQuestion` gains `AiCallMetadata`:

```csharp
public interface IQuestionGenerator
{
    Task<GeneratedQuestion> GenerateQuestionAsync(
        GenerateQuestionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record GeneratedQuestion(string Text, string Topic, AiCallMetadata? AiMetadata);
```

Evaluation is introduced in M05:

```csharp
public interface IAnswerEvaluator
{
    Task<AnswerEvaluation> EvaluateAsync(
        EvaluateAnswerRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record EvaluateAnswerRequest(
    string TargetRole,
    SeniorityLevel SeniorityLevel,
    InterviewType InterviewType,
    string FocusArea,
    int TurnNumber,
    int QuestionCount,
    string QuestionText,
    string QuestionTopic,
    string AnswerText,
    IReadOnlyList<PreviousTurnContext> PreviousTurns);
```

AI call metadata (recorded on turn):

```csharp
public sealed record AiCallMetadata(
    string PromptVersion,
    string Provider,
    string? Model,
    int? PromptTokens,
    int? CompletionTokens);
```

## SubmitAnswer Flow

1. Read session with ETag
2. Read current turn with ETag
3. Validate state
4. Load bounded prior context
5. EvaluateAsync ← IAnswerEvaluator
6. GenerateQuestionAsync ← IQuestionGenerator (if session not complete)
7. Mutate session/turn objects in memory
8. SaveAnswerAsync ← only reached when steps 5 and 6 succeeded
9. Return response with lastEvaluation populated

## Exit Criteria

- All 4 features shipped and merged
- Real Azure OpenAI question generation works behind `IQuestionGenerator`
- Answer submission returns structured rubric-based feedback via `IAnswerEvaluator`
- Adaptive next question uses bounded prior turns as context
- AI responses are validated; malformed responses are retried once and then surface as 503
- Prompt versions are recorded separately for generation and evaluation on persisted turns
- Stub implementations remain available so CI does not require live Azure OpenAI credentials
- All existing tests pass; new unit and integration tests added
- Degradation tests verify all three AI failure points leave session state unchanged
- Architecture documentation and ADR 0010 updated

## Notes

M05 does not add final session summaries (M06), history/detail pages (M06), or dashboard analytics (M07). It stops at: adaptive questions + per-answer structured evaluation, persisted and returned through the existing API.

The evaluation result must carry enough structure for M06 summaries and M07 analytics to be computed later without re-calling the AI. Per-dimension scores are stored on the turn document, not just an overall number.
