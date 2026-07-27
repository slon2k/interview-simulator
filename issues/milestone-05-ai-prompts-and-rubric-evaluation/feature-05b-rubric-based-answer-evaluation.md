# 05b - Rubric-based answer evaluation

Phase: 2  
Milestone: 05 - AI prompts and rubric evaluation  
Type: Feature  
Status: Planned

## Summary

Introduce structured, rubric-based evaluation of user answers via a new `IAnswerEvaluator` abstraction backed by Azure OpenAI. Each submitted answer receives per-dimension scores and actionable feedback, validated against a defined schema and persisted on the turn.

## Problem and User Value

In M04, answer submission saves text and advances the interview but returns no feedback. The core value of an interview simulator is feedback quality. This feature turns each answer into structured, per-dimension feedback the user can act on, and stores it so later summaries (M06) and analytics (M07) can be computed without re-calling the AI.

## Scope

- Define the evaluation result JSON schema (per-dimension scores + overall + feedback)
- Define rubrics for each interview type:
  - technical
  - behavioral
  - system design
- Add `IAnswerEvaluator` interface and an Azure OpenAI-backed implementation
- Add a stub evaluator for tests/CI (credential-free)
- Select the rubric based on the session's interview type
- Instruct the model to return structured JSON matching the schema
- Persist the evaluation result on the existing turn document
- Return structured feedback from `POST /api/interviews/{id}/answers`
- Record the prompt version used for evaluation on the turn (coordinated with 05c)
- Add unit tests for rubric selection and result mapping
- Add integration tests behind a faked AI boundary

## Out of Scope

- Question generation (05a)
- Prompt versioning infrastructure and malformed-response handling internals (05c) — consumed here, owned there
- Aggregation of scores into a final summary (M06)
- Cross-session analytics (M07)

## Acceptance Criteria

- [ ] An evaluation JSON schema is defined (per-dimension scores, overall score, textual feedback)
- [ ] Technical, behavioral, and system design rubrics exist
- [ ] `IAnswerEvaluator` interface and Azure OpenAI implementation exist
- [ ] A stub evaluator exists and is used by default in CI/tests
- [ ] Rubric selection is driven by the session's interview type
- [ ] Answer submission returns structured feedback matching the schema
- [ ] Per-dimension scores are persisted on the turn (not just an overall number)
- [ ] The evaluation prompt version is recorded on the turn
- [ ] Evaluation failures return a clear error and leave the turn/session in a resumable state
- [ ] The public API contract shape from M04 is preserved (answer endpoint now includes feedback in its response)
- [ ] CI does not require live Azure OpenAI credentials
- [ ] Unit tests cover rubric selection and result mapping
- [ ] Integration tests cover evaluation behind a faked AI boundary
- [ ] Existing tests continue to pass

## Tasks

### [ ] Rubric and schema definition

- [ ] Define evaluation JSON schema and typed result model
- [ ] Define technical / behavioral / system design rubrics
- [ ] Add interview-type → rubric selection

### [ ] Evaluator implementation

- [ ] Add `IAnswerEvaluator` interface
- [ ] Add Azure OpenAI-backed evaluator
- [ ] Add stub evaluator for tests/CI
- [ ] Map validated AI JSON to the typed result
- [ ] Persist evaluation + prompt version on the turn
- [ ] Return feedback from the answer endpoint

### [ ] Tests

- [ ] Unit tests for rubric selection and mapping
- [ ] Integration tests behind a fake AI boundary

## Verification

- [ ] Submitting an answer to a technical interview uses the technical rubric
- [ ] Submitting an answer to a behavioral interview uses the behavioral rubric
- [ ] The answer response includes per-dimension scores and feedback
- [ ] Per-dimension scores are persisted on the turn document
- [ ] Turn records the evaluation prompt version
- [ ] With the stub configured, the flow runs without Azure OpenAI credentials
- [ ] A simulated evaluation failure does not corrupt session/turn state
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 04a - Interview API (answer submission + turn persistence) ✅
- 05a - AI question generation (shares AI client wiring and prompt-version seam)

Coordinates with:

- 05c - Prompt versioning and AI response validation/error handling

Blocks:

- 05d - End-to-end AI flow verification
- 06 - Session history and summaries (consumes stored per-dimension scores)
- 07 - Dashboard analytics (consumes stored per-dimension scores)

## Risks and Open Questions

### Risks

- Score consistency across calls may be noisy; rubric wording and temperature settings matter.
- Schema drift between prompt instructions and the typed model — must be validated (owned by 05c) to avoid runtime mapping failures.

### Open Questions

- Fixed rubric dimensions per interview type, or a shared dimension set with type-specific weighting? Default assumption: fixed dimensions per type, defined in the rubric.
- Score scale (e.g. 1–5 per dimension) — confirm during implementation and document in the schema.

## Notes

Interface introduced in M05:

```csharp
public interface IAnswerEvaluator
{
    Task<AnswerEvaluation> EvaluateAsync(
        EvaluateAnswerRequest request,
        CancellationToken cancellationToken = default);
}
```

Store per-dimension scores, not just an overall score, so M06/M07 can be computed without additional AI calls.
