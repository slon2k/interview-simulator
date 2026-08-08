# 05a - AI boundary foundation

Phase: 2  
Milestone: 05 - AI prompts and rubric evaluation  
Type: Feature  
Status: Planned

## Summary

Establish the shared AI call infrastructure that 05b and 05c are built on top of: a prompt version registry, a structured-output runner with retry logic, typed AI exceptions, context and retry options, and error routing through ProblemDetails.

No domain interfaces, no AI implementations, and no endpoint changes are introduced here. This feature only provides the seams that generation and evaluation consume.

## Problem and User Value

Without a shared boundary, each AI call site would invent its own retry logic, error handling, and prompt versioning, making failures inconsistent and historical sessions uninterpretable when prompts change.

Centralising this in one layer means:

- AI failures always degrade gracefully to 503 ProblemDetails and never corrupt session state
- Prompt versions are recorded consistently across generation and evaluation
- Retry policy is bounded and predictable
- Azure-specific exceptions never leak into controller or endpoint code

## Scope

- Add `PromptVersions` static class with version constants for all stub and real prompts
- Add `AiOptions` config section (`Ai:`) with context limits and retry counts
- Add `AiStructuredOutputRunner` (DI-registered): call → parse → validate → retry → typed exception
- Add four typed AI exceptions
- Update `InfrastructureExceptionHandler` to map typed AI exceptions to 503 ProblemDetails
- Add unit tests for the structured output runner

## Out of Scope

- Question generation implementation (05b)
- Evaluation implementation, rubrics, and schema (05c)
- Azure OpenAI client registration — already done in `Startup/OpenAIServices.cs`
- Application Insights / observability wiring (M09)

## Acceptance Criteria

- [ ] `PromptVersions` declares constants for all six prompt identifiers
- [ ] `AiOptions` config section is validated on startup
- [ ] `AiStructuredOutputRunner.RunAsync<T>` retries on malformed JSON up to `InvalidOutputRetryCount`
- [ ] `AiStructuredOutputRunner.RunAsync<T>` retries on transient provider failure up to `TransientRetryCount`
- [ ] Auth/config failures and cancellations are not retried
- [ ] Azure `RequestFailedException` is caught and wrapped inside the runner; it does not reach the global handler
- [ ] All four typed AI exceptions map to 503 ProblemDetails with a `traceId` and user-safe `detail`
- [ ] Raw Azure exception details are not exposed in ProblemDetails
- [ ] Unit tests cover all validation and retry cases listed in Tasks
- [ ] Existing tests pass

## Tasks

### Prompt registry

- [ ] Add `Features/Interviews/Ai/PromptVersions.cs`
  - `HardcodedQuestionGeneration = "hardcoded-question-generation-v1"`
  - `HardcodedAnswerEvaluation = "hardcoded-answer-evaluation-v1"`
  - `QuestionGeneration = "question-generation-v1"`
  - `EvaluationTechnical = "evaluation-technical-v1"`
  - `EvaluationBehavioral = "evaluation-behavioral-v1"`
  - `EvaluationSystemDesign = "evaluation-system-design-v1"`

### AI options

- [ ] Add `AiOptions` (config section `Ai:`) with startup validation
  - `MaxQuestionGenerationPreviousTurns` (default 3) — prior turns included in question prompt
  - `MaxEvaluationPreviousTurns` (default 2) — prior turns included in evaluation prompt
  - `MaxQuestionChars` (default 800) — truncation cap per prior question text
  - `MaxAnswerChars` (default 1200) — truncation cap per prior answer text
  - `MaxFeedbackChars` (default 500) — truncation cap per prior feedback text
  - `TransientRetryCount` (default 1)
  - `InvalidOutputRetryCount` (default 1)
- [ ] Register and validate `AiOptions` in `Startup/InterviewServices.cs`
- [ ] Add default values to `appsettings.json` under `Ai:`

### Typed AI exceptions

- [ ] `Features/Interviews/Ai/AiGenerationFailedException.cs`
- [ ] `Features/Interviews/Ai/AiEvaluationFailedException.cs`
- [ ] `Features/Interviews/Ai/AiInvalidResponseException.cs`
- [ ] `Features/Interviews/Ai/AiProviderUnavailableException.cs`

### Structured output runner

- [ ] Add `Features/Interviews/Ai/AiCallMetadata.cs`
  - Properties: `string PromptVersion`, `string Provider`, `string? Model`, `int? PromptTokens`, `int? CompletionTokens`
- [ ] Add `Features/Interviews/Ai/AiRawResponse.cs` — `string Content`, `AiCallMetadata Metadata`
- [ ] Add `Features/Interviews/Ai/AiStructuredOutput.cs` — `T Value`, `AiCallMetadata Metadata`
- [ ] Add `Features/Interviews/Ai/AiStructuredOutputRunner.cs` (registered as scoped)
  - Signature: `RunAsync<T>(string operationName, Func<CancellationToken, Task<AiRawResponse>> callAsync, Func<T, IReadOnlyList<string>> validate, CancellationToken ct) → Task<AiStructuredOutput<T>>`
  - Attempt: `System.Text.Json` deserialise response content, run `validate` delegate
  - On malformed JSON or non-empty validation errors: retry up to `InvalidOutputRetryCount`; then throw `AiInvalidResponseException`
  - On Azure `RequestFailedException`: retry up to `TransientRetryCount`; then wrap in `AiProviderUnavailableException`
  - On `OperationCanceledException`: rethrow without retry

### Error routing

- Extend `Features/Common/InfrastructureExceptionHandler.cs`
- `AiGenerationFailedException` → 503
- `AiEvaluationFailedException` → 503
- `AiInvalidResponseException` → 503
- `AiProviderUnavailableException` → 503
- ProblemDetails `detail`: "The AI service could not complete the request. Please retry."
- ProblemDetails `type`: stable URI, e.g. `https://interviewsimulator/errors/ai-unavailable`

### Tests

- `api/tests/.../Ai/AiStructuredOutputRunnerTests.cs`
- Valid JSON + passing validator → returns `AiStructuredOutput<T>`
- Malformed JSON → retries once, succeeds on second attempt → returns result
- Malformed JSON twice → throws `AiInvalidResponseException`
- Validator returns errors → retries once, fails again → throws `AiInvalidResponseException`
- Azure `RequestFailedException` (transient) → retries once, succeeds → returns result
- Azure `RequestFailedException` twice → throws `AiProviderUnavailableException`
- `OperationCanceledException` → rethrown, no retry

## Verification

- [ ] Startup validates `AiOptions`; missing or invalid values prevent app start
- [ ] A simulated malformed AI response is retried once and then surfaces as 503 ProblemDetails
- [ ] Raw Azure exception message is not present in the ProblemDetails response body
- [ ] `PromptVersions` constants follow the `{purpose}-v{N}` convention
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 04a - Interview API (establishes `InfrastructureExceptionHandler` baseline and turn persistence) ✅
- Azure OpenAI client registration in `Startup/OpenAIServices.cs` ✅

Blocks:

- 05b - Azure OpenAI question generation
- 05c - Rubric-based answer evaluation

## Risks and Open Questions

### Risks

- Retry on transient failure multiplies latency; `TransientRetryCount = 1` is intentionally conservative for MVP.

### Open Questions

- Should `AiOptions` live in its own `Ai:` config section (provider-neutral) or extend the existing `AzureOpenAI:` section? Default assumption: separate `Ai:` section. Confirm before implementing.
