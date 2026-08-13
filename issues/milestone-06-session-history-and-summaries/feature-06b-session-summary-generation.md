# 06b - Session summary generation

Phase: 2  
Milestone: 06 - Session history and summaries  
Type: Feature  
Status: Planned

## Summary

Generate a one-time AI summary when an interview is completed, using the stored per-turn evaluations as input, and persist it on the session. Summaries are produced once at completion — never on read.

## Problem and User Value

After finishing an interview, a user benefits from a short overall assessment: strengths, weaknesses, and a headline takeaway across all answers. This feature produces that summary from the per-dimension scores and feedback already stored in M05, so it requires no new evaluation calls per answer — one summarization call per completed session.

## Scope

- Add a single-purpose summarizer abstraction (`ISessionSummarizer`) with an Azure OpenAI implementation and a stub for tests/CI
- Trigger summary generation after session is saved as Completed (best-effort second save via `UpdateSessionAsync`)
- Build the summary prompt from stored turn evaluations (per-dimension scores + feedback), not by re-evaluating answers
- Persist summary as `InterviewSummary` and `SummaryMetadata` on `CosmosSessionDocument` (`summary` and `summaryAi` fields)
- Prompt version stored in `summaryAi.promptVersion`, not inside `InterviewSummary`
- Reuse the M05 AI boundary: prompt versioning, response validation, graceful failure handling
- Ensure summary failure does not block or reverse completion (session stays completed and reviewable with its score)
- Support regeneration via `POST /api/interviews/{id}/summary` (requires `Completed` status; replaces existing summary)
- Register `ISessionSummarizer` stub in `AuthWebApplicationFactory`
- Add unit tests for prompt construction and failure handling
- Add integration tests behind a faked AI boundary

## Out of Scope

- Detail read API (06a)
- UI (06c)
- Cross-session analytics (M07)
- Re-running per-answer evaluation

## Acceptance Criteria

- [ ] An `ISessionSummarizer` abstraction exists with real and stub implementations
- [ ] `ISessionSummarizer` stub registered in `AuthWebApplicationFactory`
- [ ] Completing a session generates and persists a summary (best-effort after `SaveAnswerAsync`)
- [ ] Summary generation is a separate `UpdateSessionAsync` call; failure does not roll back completion or score
- [ ] The summary is built from stored per-turn evaluations (no per-answer re-evaluation)
- [ ] Summary prompt version is stored in `summaryAi.promptVersion` on `CosmosSessionDocument`
- [ ] Summary generation reuses the M05 validation/error-handling boundary
- [ ] A completed session without a summary can have one (re)generated via `POST /api/interviews/{id}/summary`
- [ ] Viewing a session never triggers summary generation
- [ ] CI does not require live Azure OpenAI credentials
- [ ] Unit tests cover prompt construction and failure handling
- [ ] Integration tests cover generation behind a faked AI boundary
- [ ] Existing tests continue to pass

## Tasks

### [ ] Summarizer implementation

- [ ] Add `ISessionSummarizer` interface
- [ ] Add Azure OpenAI-backed summarizer + stub; register stub in `AuthWebApplicationFactory`
- [ ] Build summary prompt from stored evaluations
- [ ] Trigger summary after `SaveAnswerAsync` completes (best-effort `UpdateSessionAsync`)
- [ ] Handle summary failure without blocking completion or affecting score
- [ ] Add `POST /api/interviews/{id}/summary` for regeneration (requires `Completed`; replaces existing summary)

### [ ] Tests

- [ ] Unit tests for prompt build and failure paths
- [ ] Integration tests behind a fake AI boundary

## Verification

- [ ] Completing a session persists a summary
- [ ] The summary reflects stored per-dimension scores/feedback
- [ ] No per-answer re-evaluation occurs during summarization
- [ ] A simulated summary failure leaves the session completed and reviewable
- [ ] Regeneration produces/updates the summary
- [ ] Stub path runs without Azure OpenAI credentials
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 05c - Rubric-based answer evaluation (stored per-dimension scores as summary input) ✅
- 05a - Prompt versioning and AI response validation/error handling (reused boundary) ✅
- 06a - Session detail read API (provides `SessionResult`/`InterviewSummary` domain types and session detail query)

Blocks:

- 06c - History and session detail UI (displays the summary)

## Risks and Open Questions

### Risks

- Coupling completion to an AI call adds latency/failure surface to the complete action; failure handling must keep completion fast and reliable (summary is best-effort).

### Open Questions

Decided: synchronous best-effort. Score is committed first via `SaveAnswerAsync`; summary attempt follows as a separate `UpdateSessionAsync`. Summary failure is logged and ignored — the HTTP response returns the completed session with its score regardless.

## Notes

`ISessionSummarizer` is deliberately separate from `IQuestionGenerator` and `IAnswerEvaluator` — three narrow capabilities, three interfaces, consistent with the project's interface-per-capability approach. It is not merged into a single "AI service".
