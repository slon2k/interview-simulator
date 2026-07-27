# 05c - Prompt versioning and AI response validation/error handling

Phase: 2  
Milestone: 05 - AI prompts and rubric evaluation  
Type: Feature  
Status: Planned

## Summary

Provide the cross-cutting AI-boundary hardening that 05a and 05b depend on: a prompt versioning mechanism, JSON response validation against the evaluation schema, and consistent error handling so AI failures degrade gracefully without corrupting interview state.

## Problem and User Value

Real AI calls fail, return malformed JSON, or drift as prompts are edited over time. Without a shared discipline for this, each AI call site would handle failures differently and historical sessions would become uninterpretable after a prompt change.

This feature centralizes:

- **Prompt versioning** so every generated question and evaluation is traceable to the exact prompt that produced it
- **Response validation** so malformed AI output is caught at the boundary, not deep in mapping code
- **Error handling** so a failed AI call returns a clear, resumable error instead of a corrupt turn

## Scope

- Define a prompt version identifier convention and a place prompts/versions live
- Record the prompt version on persisted turns (used by both 05a generation and 05b evaluation)
- Validate AI JSON responses against the evaluation schema at the AI boundary
- Define behavior for malformed/invalid responses:
  - single bounded retry (optional, configurable)
  - then a clear, typed failure that keeps the session resumable
- Standardize AI error surfacing through the existing `ProblemDetails` error shape (`Startup/Diagnostics.cs`)
- Ensure AI failures never leave partially-written turn state
- Add unit tests for validation (valid, malformed, missing-fields) and version stamping
- Add integration tests for the failure/degradation path behind a faked AI boundary

## Out of Scope

- The generation prompt content itself (05a)
- The rubric content and evaluation schema definition (05b) — this feature validates against that schema, it does not define the rubric
- Observability/Application Insights wiring (M09)
- Per-user usage/cost limits (M09)

## Acceptance Criteria

- [ ] A prompt version identifier convention is defined and documented
- [ ] Prompt versions are recorded on persisted turns for both generation and evaluation
- [ ] AI JSON responses are validated against the evaluation schema at the boundary
- [ ] Malformed/invalid AI responses are detected and do not reach mapping/persistence
- [ ] Malformed responses trigger the defined handling (optional bounded retry, then typed failure)
- [ ] AI failures surface through the existing `ProblemDetails` shape with a `traceId`
- [ ] A failed AI call leaves the session/turn in a resumable state (no partial writes)
- [ ] Behavior is consistent across question generation and answer evaluation
- [ ] CI does not require live Azure OpenAI credentials
- [ ] Unit tests cover valid, malformed, and missing-field responses
- [ ] Unit tests cover version stamping
- [ ] Integration tests cover the degradation path
- [ ] Existing tests continue to pass

## Tasks

### [ ] Prompt versioning

- [ ] Define prompt version identifier convention
- [ ] Establish where prompts and their versions are declared
- [ ] Stamp prompt version onto persisted turns

### [ ] Validation and error handling

- [ ] Add response validation against the evaluation schema
- [ ] Define malformed-response handling (bounded retry + typed failure)
- [ ] Route AI failures through `ProblemDetails`
- [ ] Guarantee no partial turn writes on failure

### [ ] Tests

- [ ] Unit tests: valid / malformed / missing-field responses
- [ ] Unit tests: version stamping
- [ ] Integration tests: degradation path

## Verification

- [ ] Turns record the prompt version for both generation and evaluation
- [ ] A malformed AI JSON response is rejected and does not persist a bad turn
- [ ] A malformed response produces a clear `ProblemDetails` error with `traceId`
- [ ] After an AI failure the interview can be resumed
- [ ] Changing a prompt produces a new recorded version on subsequent turns
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 04a - Interview API (turn persistence) ✅
- `ProblemDetails` error shape from `Startup/Diagnostics.cs` ✅

Coordinates with:

- 05a - AI question generation (consumes version stamping + error handling)
- 05b - Rubric-based answer evaluation (consumes validation against its schema)

Blocks:

- 05d - End-to-end AI flow verification

## Risks and Open Questions

### Risks

- Retry logic can multiply token cost and latency; retries must be bounded and configurable.
- Over-strict validation could reject usable responses; schema should require only fields the app actually consumes.

### Open Questions

- Retry once on malformed JSON, or fail fast? Default assumption: one bounded retry, then typed failure. Confirm during implementation.
- Where do prompts live — in code constants, embedded resources, or config? Default assumption: versioned in code, referenced by identifier. Candidate for a short ADR.

## Notes

This feature is the shared AI boundary. 05a and 05b should not each invent their own failure handling — they call through the seams defined here.

Suggested ADR: "AI response validation and prompt versioning strategy" (next number: ADR 0009).
