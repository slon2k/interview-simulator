# 05c - Rubric-based answer evaluation

Phase: 2  
Milestone: 05 - AI prompts and rubric evaluation  
Type: Feature  
Status: Planned

## Summary

Introduce `IAnswerEvaluator` backed by Azure OpenAI to score each submitted answer using a per-interview-type rubric. The evaluation result, including per-dimension scores and feedback, is persisted on the turn and returned in the answer endpoint response.

This feature builds on the AI boundary infrastructure from 05a and shares the Azure OpenAI client wiring from 05b.

## Problem and User Value

M04 records answers but returns no feedback. The core value of an interview simulator is actionable, per-answer feedback. This feature provides:

- Structured per-dimension scores the user can act on
- Persisted evaluation data so M06 summaries and M07 analytics can be computed without re-calling the AI
- The same graceful degradation guarantees as question generation — an evaluation failure never corrupts session state

## Scope

- Define `IAnswerEvaluator` interface and `EvaluateAnswerRequest`
- Add rubric definitions for Technical, Behavioral, and SystemDesign interview types with stable dimension keys
- Add an Azure OpenAI-backed `AzureOpenAIAnswerEvaluator` using `AiStructuredOutputRunner` from 05a
- Add a `StubAnswerEvaluator` for tests and CI
- Expand `AnswerEvaluation` domain type with per-dimension scores; `OverallScore` is computed by the app, not returned by AI
- Expand `CosmosEvaluationDocument` with dimensions; old documents without dimensions remain readable
- Add `EvaluationContract` and `EvaluationDimensionContract` to the interview response contract; expose as `lastEvaluation` on `InterviewResponse`
- Add `AnswerEvaluationMetadata` to `InterviewTurn` and `CosmosTurnDocument` (separate from generation metadata)
- Update `SubmitAnswer` to evaluate before persisting; on AI failure leave session state unchanged
- Select evaluator via `Ai:Provider` config, same flag as question generator
- Update `AuthWebApplicationFactory` to register `StubAnswerEvaluator` for `IAnswerEvaluator`

## Out of Scope

- Question generation (05b)
- AI boundary infrastructure (05a) — consumed here, not defined here
- Aggregation of per-answer scores into a final summary (M06)
- Cross-session analytics (M07)
- Voice evaluation (M08)

## Acceptance Criteria

- [ ] `IAnswerEvaluator` interface and `EvaluateAnswerRequest` are defined
- [ ] Technical, Behavioral, and SystemDesign rubrics exist with stable dimension keys and display labels
- [ ] `AzureOpenAIAnswerEvaluator` and `StubAnswerEvaluator` exist
- [ ] Rubric selection is driven by the session's interview type
- [ ] `AnswerEvaluation` carries per-dimension scores (`Key`, `Label`, `Score 0-100`, `Feedback`)
- [ ] `OverallScore` is computed as the rounded integer average of dimension scores by the app (not from AI)
- [ ] `CosmosEvaluationDocument` stores dimensions; old documents without dimensions are still readable
- [ ] Evaluation metadata is stored separately from generation metadata (`evaluationAi` JSON property)
- [ ] `InterviewResponse.LastEvaluation` carries `TurnNumber`, `OverallScore`, `MaxScore = 100`, `Feedback`, and all dimension scores
- [ ] `InterviewResponse.Feedback` (session-level `FeedbackContract`) remains null in M05; reserved for M06
- [ ] `SubmitAnswer` calls evaluation before `SaveAnswerAsync`; on AI failure `SaveAnswerAsync` is not called
- [ ] Next-question generation failure after successful evaluation also prevents `SaveAnswerAsync` being called
- [ ] CI does not require live Azure OpenAI credentials
- [ ] `AuthWebApplicationFactory` registers `StubAnswerEvaluator` for `IAnswerEvaluator`
- [ ] Unit and integration tests pass

## SubmitAnswer flow (canonical — no AI result, no Cosmos write)

1. Read session with ETag
2. Read current turn with ETag
3. Validate state
4. Load bounded prior context
5. EvaluateAsync ← IAnswerEvaluator
6. GenerateQuestionAsync ← IQuestionGenerator (if session not complete)
7. Mutate session/turn objects in memory
8. SaveAnswerAsync ← only reached when steps 5 and 6 succeeded
9. Return response with lastEvaluation populated

## Tasks

### `IAnswerEvaluator` interface and request type

- [ ] Add `Features/Interviews/IAnswerEvaluator.cs`
  - `Task<AnswerEvaluation> EvaluateAsync(EvaluateAnswerRequest request, CancellationToken cancellationToken = default)`
- [ ] Add `EvaluateAnswerRequest` record:
  - `string TargetRole`
  - `SeniorityLevel SeniorityLevel`
  - `InterviewType InterviewType`
  - `string FocusArea`
  - `int TurnNumber`
  - `int QuestionCount`
  - `string QuestionText`
  - `string QuestionTopic`
  - `string AnswerText`
  - `IReadOnlyList<PreviousTurnContext> PreviousTurns`
- [ ] Add `PreviousTurnContext` record:
  - `int TurnNumber`, `string QuestionText`, `string QuestionTopic`, `string AnswerText`, `int? OverallScore`, `string? Feedback`

### Rubric definitions

- [ ] Add `Features/Interviews/Ai/Rubrics.cs`
  - Each dimension: `(string Key, string Label, string Description)`
  - Technical: `technicalCorrectness`, `depth`, `communication`, `problemSolving`
  - Behavioral: `situationContext`, `actionTaken`, `result`, `reflection`
  - SystemDesign: `requirementsClarity`, `componentDesign`, `scalability`, `tradeoffs`
  - `GetRubric(InterviewType) → IReadOnlyList<RubricDimension>` — used in prompt building and response validation

### Expand `AnswerEvaluation` domain type

- [ ] In `Features/Interviews/InterviewTurn.cs`:
  - Rename `Score` → `OverallScore` on `AnswerEvaluation`
  - Add `IReadOnlyList<EvaluationDimension> Dimensions`
- [ ] Add `EvaluationDimension(string Key, string Label, int Score, string Feedback)` record — `Score` 0–100

### Expand `InterviewTurn` domain model

- [ ] Add `AiCallMetadata? AnswerEvaluationMetadata { get; private set; }` property
- [ ] Add `RecordEvaluation(AnswerEvaluation evaluation, AiCallMetadata? metadata)` method — sets `Evaluation` and `AnswerEvaluationMetadata`

### Expand `CosmosTurnDocument`

- [ ] Expand `CosmosEvaluationDocument`:
  - Add `List<CosmosEvaluationDimensionDocument>? Dimensions` (nullable — backwards compatible)
- [ ] Add `CosmosEvaluationDimensionDocument(string Key, string Label, int Score, string Feedback)`
- [ ] Add `CosmosAiMetadataDocument? EvaluationAiMetadata` (serialised as `"evaluationAi"`) — added alongside `QuestionAiMetadata` from 05b
- [ ] Update `CosmosInterviewStore` mapping to wire `EvaluationAiMetadata` and `Dimensions`

### Update interview response contract

- [ ] Add `EvaluationDimensionContract(string Key, string Label, int Score, int MaxScore, string Feedback)` — `MaxScore` always 100
- [ ] Add `EvaluationContract(int TurnNumber, int OverallScore, int MaxScore, string Feedback, IReadOnlyList<EvaluationDimensionContract> Dimensions)`
- [ ] Add `EvaluationContract? LastEvaluation` to `InterviewResponse` (nullable, additive)
- [ ] Update `InterviewContractsMapping` to map `AnswerEvaluation` including dimensions, keys, and labels

### Implement `AzureOpenAIAnswerEvaluator`

- [ ] Add `Features/Interviews/AzureOpenAIAnswerEvaluator.cs`
  - Selects rubric via `Rubrics.GetRubric(request.InterviewType)`
  - Slices prior turns to last `AiOptions.MaxEvaluationPreviousTurns`; truncates text fields per char caps
  - Expected JSON: `{ "dimensions": [{"key": "...", "score": N, "feedback": "..."}], "feedback": "..." }` — AI does not return overall score
  - `OverallScore = (int)Math.Round(dimensions.Average(d => d.Score))`
  - Uses `AiStructuredOutputRunner` for call, parse, validate, retry
  - Returns `AnswerEvaluation` with dimensions, overall score, feedback, and evaluation AI metadata

### Add explicit response validator

- [ ] Add `EvaluationResponseValidator.Validate(response, rubric)` — returns `IReadOnlyList<string>` error list
  - All rubric dimension keys must be present in the response
  - Each dimension score must be 0–100
  - `feedback` field required and non-empty

### Implement `StubAnswerEvaluator`

- [ ] Add `Features/Interviews/StubAnswerEvaluator.cs`
  - Returns deterministic `AnswerEvaluation` for each interview type with all rubric dimensions at fixed scores
  - `AiCallMetadata` set to `AiCallMetadata(PromptVersions.HardcodedAnswerEvaluation, "Hardcoded", null, null, null)`

### Update `SubmitAnswer`

- [ ] Inject `IAnswerEvaluator`
- [ ] Build `EvaluateAnswerRequest` from session + current turn + bounded prior turns
- [ ] Call `EvaluateAsync` — on typed AI exception: let propagate; do not call `SaveAnswerAsync`
- [ ] Call `GenerateQuestionAsync` if needed — on typed AI exception: let propagate; do not call `SaveAnswerAsync`; evaluation result is discarded
- [ ] Call `currentTurn.RecordEvaluation(evaluation, evaluation metadata)` and `RecordQuestionGenerationMetadata` in memory after both AI calls succeed
- [ ] Call `SaveAnswerAsync` only when all AI calls succeeded
- [ ] Populate `LastEvaluation` in the response from `currentTurn.Evaluation`

### Wire config-driven selection

- [ ] Update `Startup/InterviewServices.cs`:
  - `Ai:Provider = Hardcoded` → register `StubAnswerEvaluator`
  - `Ai:Provider = AzureOpenAI` → register `AzureOpenAIAnswerEvaluator`
- [ ] Update `AuthWebApplicationFactory` to register `StubAnswerEvaluator` for `IAnswerEvaluator`

### Tests

- [ ] `RubricTests.cs` (unit)
  - `GetRubric(Technical)` returns exactly 4 dimensions with keys `technicalCorrectness`, `depth`, `communication`, `problemSolving`
  - `GetRubric(Behavioral)` returns `situationContext`, `actionTaken`, `result`, `reflection`
  - `GetRubric(SystemDesign)` returns `requirementsClarity`, `componentDesign`, `scalability`, `tradeoffs`
- [ ] `EvaluationContractMappingTests.cs` (unit)
  - `AnswerEvaluation` with dimensions maps to `EvaluationContract` preserving all keys, labels, scores, and `MaxScore = 100`
  - `OverallScore` is correctly present on contract
- [ ] `StubAnswerEvaluatorTests.cs` (unit)
  - Returns correct dimension count per interview type
  - All dimension scores are in 0–100 range
  - `AiMetadata.PromptVersion` equals `PromptVersions.HardcodedAnswerEvaluation`
- [ ] `SubmitAnswerEvaluationTests.cs` (integration)
  - Answer response includes `lastEvaluation` with correct dimension keys and non-null scores
  - `EvaluationAiMetadata.PromptVersion` is recorded on the turn document
  - Simulated evaluator failure (throws `AiEvaluationFailedException`): response is 503, session stays `Active`, turn remains unanswered
  - Simulated evaluator success + generator failure (throws `AiGenerationFailedException`): response is 503, session stays `Active`, turn remains unanswered

## Verification

- [ ] Submitting an answer to a technical interview uses the technical rubric (4 dimensions in response)
- [ ] Submitting an answer to a behavioral interview uses the behavioral rubric
- [ ] The answer response includes `lastEvaluation.dimensions` with per-dimension scores
- [ ] `evaluationAi.promptVersion` is present on the persisted turn document
- [ ] `questionAi` and `evaluationAi` are stored as separate fields on the turn document
- [ ] `feedback` (session-level) is null on all responses in M05
- [ ] With `Ai:Provider = Hardcoded`, the full flow runs without Azure OpenAI credentials
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 05a - AI boundary foundation (defines `AiStructuredOutputRunner`, `AiOptions`, `PromptVersions`, typed exceptions)
- 05b - Azure OpenAI question generation (shares `CosmosTurnDocument` AI metadata split)
- 04a - Interview API (answer submission and turn persistence) ✅

Blocks:

- 05d - End-to-end AI flow verification
- 06 - Session history and summaries (consumes stored per-dimension scores)
- 07 - Dashboard analytics (consumes stored per-dimension scores)

## Risks and Open Questions

### Risks

- Score consistency across AI calls may be noisy; rubric wording and temperature settings affect variance.
- Adding `LastEvaluation` to `InterviewResponse` is an additive contract change; existing API clients that do not read the new field are unaffected.

### Open Questions

- Should `OverallScore` be a simple average or a weighted average? Default assumption: unweighted rounded average for M05. Weighting can be added to the rubric definition in a later milestone if needed.
- Should `PreviousTurnContext.OverallScore` and `PreviousTurnContext.Feedback` be included only when available (nullable)? Yes — null-safe handling required on first-run sessions where no prior evaluations exist.
