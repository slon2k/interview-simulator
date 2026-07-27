# Milestone 05 - AI prompts and rubric evaluation (placeholder)

Epic type: Milestone

## Overview

Replace the M04 hardcoded stubs with real Azure OpenAI integration. This milestone introduces adaptive AI question generation and structured, rubric-based answer evaluation while keeping the interview lifecycle and API contract established in M04 unchanged.

M04 proved the full session lifecycle works end-to-end with deterministic stubs behind `IQuestionGenerator`. M05 swaps in real generation, adds an `IAnswerEvaluator` alongside it, and hardens the AI boundary with response validation, error handling, and prompt versioning so that AI failures degrade gracefully instead of breaking the interview flow.

The public API surface (`/api/interviews/*`) does not change shape. What changes is that questions become context-aware and answers now return structured feedback instead of nothing.

## Feature Issues

- 05a - AI question generation (real `IQuestionGenerator`)
- 05b - Rubric-based answer evaluation (`IAnswerEvaluator` + rubric schema)
- 05c - Prompt versioning and AI response validation/error handling
- 05d - End-to-end AI flow verification and documentation

## Key Decisions

- **Reuse M04 abstractions**: `IQuestionGenerator` gets a real Azure OpenAI implementation; the stub is retained for tests and offline/CI runs. Selection is config-driven, not a code change per environment.
- **New abstraction for evaluation**: `IAnswerEvaluator` is a separate, single-purpose interface — question generation and answer scoring are distinct capabilities and must not be merged into one "AI service".
- **Structured output**: evaluation returns a typed result validated against a defined JSON schema, not free-form text. AI is instructed to return JSON; the app validates and rejects/repairs malformed responses.
- **Rubrics per interview type**: technical, behavioral, and system design each have their own rubric. The interview type on the session selects the rubric.
- **Prompt versioning**: every generated question and every evaluation records the prompt version used, persisted on the turn, so historical sessions remain interpretable when prompts change.
- **Graceful degradation**: an AI failure never corrupts interview state. Generation/evaluation failures return a clear error and leave the session in a resumable state.
- **Persistence unchanged**: continue using `CosmosSessionDocument` and `CosmosTurnDocument`. Evaluation output and prompt version are stored on the existing turn document; no new container is introduced.

## Interface Shape

Generation reuses the M04 interface (implementation changes, contract does not):

```csharp
public interface IQuestionGenerator
{
    Task<GeneratedQuestion> GenerateQuestionAsync(
        GenerateQuestionRequest request,
        CancellationToken cancellationToken = default);
}
```

Evaluation is introduced in M05:

```csharp
public interface IAnswerEvaluator
{
    Task<AnswerEvaluation> EvaluateAsync(
        EvaluateAnswerRequest request,
        CancellationToken cancellationToken = default);
}
```

## Exit Criteria

- All 4 features shipped and merged
- Real Azure OpenAI question generation works behind `IQuestionGenerator`
- Answer submission returns structured rubric-based feedback via `IAnswerEvaluator`
- Adaptive next question uses prior turns as context
- AI responses are validated; malformed responses are handled without corrupting session state
- Prompt versions are recorded on persisted turns
- Stub implementations remain available so CI does not require live Azure OpenAI credentials
- All existing tests pass; new unit and integration tests added
- Architecture documentation and ADRs updated

## Notes

M05 does not add final session summaries (M06), history/detail pages (M06), or dashboard analytics (M07). It stops at: adaptive questions + per-answer structured evaluation, persisted and returned through the existing API.

The evaluation result should carry enough structure for M06 summaries and M07 analytics to be computed later without re-calling the AI — i.e. per-dimension scores are stored, not just an overall number.
