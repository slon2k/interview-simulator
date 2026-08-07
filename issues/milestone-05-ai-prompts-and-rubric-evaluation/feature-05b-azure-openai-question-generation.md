# 05b - Azure OpenAI question generation

Phase: 2  
Milestone: 05 - AI prompts and rubric evaluation  
Type: Feature  
Status: Planned

## Summary

Replace the M04 hardcoded `IQuestionGenerator` stub with a real Azure OpenAI implementation that produces adaptive, context-aware questions. The stub is retained and remains the default in CI. Selection is config-driven.

This feature builds directly on the AI boundary infrastructure established in 05a.

## Problem and User Value

M04 delivered the full interview lifecycle with deterministic hardcoded questions. Questions are the same regardless of role, topic, seniority, or prior answers, which is not a usable interview experience.

This feature makes questions:
- Relevant to the selected role, seniority, topic, and interview type
- Adaptive — later questions take prior turns into account
- Varied across sessions instead of a fixed script

## Scope

- Add a real Azure OpenAI implementation of the existing `IQuestionGenerator` interface
- Keep `HardcodedQuestionGenerator` available for tests and offline/CI runs
- Select the active implementation via `Ai:Provider` configuration
- Build the generation prompt from session setup and bounded prior turns
- Use `AiStructuredOutputRunner` from 05a for call, parse, validate, retry, and error handling
- Add `AiCallMetadata` to `GeneratedQuestion` so callers can stamp it onto the persisted turn
- Add separate `QuestionGenerationMetadata` to `InterviewTurn` and `CosmosTurnDocument`
- Stamp question generation metadata on turns in `StartInterview` and `SubmitAnswer`
- Add unit tests for prompt construction and context window logic
- Add unit tests for config-driven selection

## Out of Scope

- Answer evaluation (05c)
- AI boundary infrastructure (05a) — this feature consumes those seams
- UI changes — question contract is unchanged
- Final summaries (M06)

## Acceptance Criteria

- [ ] A real Azure OpenAI implementation of `IQuestionGenerator` exists
- [ ] `HardcodedQuestionGenerator` remains and is the default when `Ai:Provider = Hardcoded`
- [ ] `Ai:Provider = AzureOpenAI` selects `AzureOpenAIQuestionGenerator`
- [ ] Generated questions reflect role, seniority, topic, and interview type
- [ ] Prior turns are included as context, capped at `AiOptions.MaxQuestionGenerationPreviousTurns`
- [ ] Answer text in context is truncated at `AiOptions.MaxAnswerChars`
- [ ] The question prompt version is recorded on the persisted turn (`QuestionGenerationMetadata`)
- [ ] `CosmosTurnDocument` stores generation metadata separately from evaluation metadata (`questionAi` JSON property)
- [ ] Generation failures throw typed AI exceptions; `StartInterview` and `SubmitAnswer` leave session state unchanged
- [ ] CI does not require live Azure OpenAI credentials
- [ ] Unit tests cover prompt construction, context window capping, answer truncation, and config selection
- [ ] Existing tests pass

## Tasks

### [ ] Expand `GeneratedQuestion` and `IQuestionGenerator`

- [ ] Add `AiCallMetadata? AiMetadata` to `GeneratedQuestion` record
- [ ] Update `HardcodedQuestionGenerator` to return `AiCallMetadata(PromptVersions.HardcodedQuestionGeneration, "Hardcoded", null, null, null)`

### [ ] Expand `InterviewTurn` domain model

- [ ] Add `AiCallMetadata? QuestionGenerationMetadata { get; private set; }` property
- [ ] Add `RecordQuestionGenerationMetadata(AiCallMetadata metadata)` method

### [ ] Expand `CosmosTurnDocument`

- [ ] Replace single `AiMetadata` field with two fields:
  - `CosmosAiMetadataDocument? QuestionAiMetadata` (serialised as `"questionAi"`)
  - `CosmosAiMetadataDocument? EvaluationAiMetadata` (serialised as `"evaluationAi"`) — populated in 05c
- [ ] Update `CosmosInterviewStore` mapping to wire `QuestionAiMetadata` from/to `InterviewTurn.QuestionGenerationMetadata`

### [ ] Implement `AzureOpenAIQuestionGenerator`

- [ ] Add `Features/Interviews/AzureOpenAIQuestionGenerator.cs`
  - Takes `AzureOpenAIClient`, `AzureOpenAIOptions`, `AiStructuredOutputRunner`, `AiOptions`
  - Selects the deployment name from `AzureOpenAIOptions`
  - Slices prior turns to last `MaxQuestionGenerationPreviousTurns`; truncates question text at `MaxQuestionChars` and answer text at `MaxAnswerChars`
  - Expected JSON: `{ "text": "...", "topic": "..." }`
  - Validator: `text` and `topic` required and non-empty; rejects markdown-envelope wrapping
  - Throws `AiGenerationFailedException` only if the runner throws (runner handles retries internally)
  - Returns `GeneratedQuestion` with `AiCallMetadata(PromptVersions.QuestionGeneration, "AzureOpenAI", model, promptTokens, completionTokens)`

### [ ] Add explicit response validator

- [ ] Add `QuestionGenerationResponseValidator.Validate(response)` — returns `IReadOnlyList<string>` error list
  - `text` required and non-whitespace
  - `topic` required and non-whitespace

### [ ] Wire config-driven selection

- [ ] Update `Startup/InterviewServices.cs`
  - Read `Ai:Provider` (`Hardcoded` / `AzureOpenAI`); default `Hardcoded`
  - Register `HardcodedQuestionGenerator` or `AzureOpenAIQuestionGenerator` accordingly

### [ ] Stamp generation metadata on turns

- [ ] Update `StartInterview.cs` — after `GenerateQuestionAsync`, call `firstTurn.RecordQuestionGenerationMetadata(result.AiMetadata)` if metadata present
- [ ] Update `SubmitAnswer.cs` — after `GenerateQuestionAsync`, call `nextTurn.RecordQuestionGenerationMetadata(result.AiMetadata)` if metadata present

### [ ] Tests

- [ ] `AzureOpenAIQuestionGeneratorTests.cs` (unit)
  - Prompt text includes target role, seniority level, interview type, and focus area
  - Prior turns are sliced to `MaxQuestionGenerationPreviousTurns`; excess turns are excluded
  - Answer text longer than `MaxAnswerChars` is truncated before being added to the prompt
  - `QuestionGenerationResponseValidator.Validate` returns error for missing `text` field
  - `QuestionGenerationResponseValidator.Validate` returns error for missing `topic` field
- [ ] Config-driven selection test — `Ai:Provider = Hardcoded` registers `HardcodedQuestionGenerator`; `Ai:Provider = AzureOpenAI` registers `AzureOpenAIQuestionGenerator`
- [ ] Verify existing integration tests pass without modification (factory already overrides `IQuestionGenerator`)

## Verification

- [ ] Starting an interview with `Ai:Provider = AzureOpenAI` produces a context-appropriate question
- [ ] Subsequent questions reference prior answers when prior turns are present in context
- [ ] Turn documents store `questionAi.promptVersion`
- [ ] With `Ai:Provider = Hardcoded`, the full flow runs without Azure OpenAI credentials
- [ ] A simulated generation failure in `StartInterview` leaves the session in `Created` state
- [ ] A simulated generation failure in `SubmitAnswer` leaves the session `Active` with the previous turn unanswered
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 05a - AI boundary foundation (defines `AiStructuredOutputRunner`, `AiOptions`, `PromptVersions`, typed exceptions)
- 04a - Interview API (defines `IQuestionGenerator`, `GeneratedQuestion`, turn persistence) ✅
- Azure OpenAI access validated in M01 ✅

Blocks:

- 05d - End-to-end AI flow verification

## Risks and Open Questions

### Risks

- Context prompts that include full history can grow token cost quickly; `MaxQuestionGenerationPreviousTurns` and char caps mitigate this.
- Non-deterministic output makes exact-text assertions unreliable; tests should assert structure and shape, not wording.

### Open Questions

- Should question topic be derived by the AI or constrained to the session `FocusArea`? Default assumption: AI returns a topic string; it is expected to align with `FocusArea` through the prompt, not validated by the app.
