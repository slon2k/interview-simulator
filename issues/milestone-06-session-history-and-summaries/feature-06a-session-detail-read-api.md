# 06a - Session detail read API

Phase: 2  
Milestone: 06 - Session history and summaries  
Type: Feature  
Status: Completed

## Summary

Introduce the domain and storage shape for session-level results and summaries, then add the detail read path. M04 deliberately kept the detail endpoint shallow; this feature completes it and lays the foundation that 06b builds on.

## Problem and User Value

M04 stored full turn history but `GET /api/interviews/{id}` only returned enough state to resume an active interview, not the full answer/score/feedback history. Users cannot yet review a finished interview. This feature exposes the stored history for review.

## Scope

### Domain and storage shape (prerequisite for 06b)

- Refactor `InterviewFeedback(int Score, string? Summary)` into two separate session properties:
  - `SessionResult(int OverallScore)` — deterministic aggregate, stored after every answered turn
  - `InterviewSummary(string Text, DateTimeOffset CreatedAt)` — AI narrative placeholder, populated by 06b
  - `AiCallMetadata? SummaryMetadata` — AI metadata placeholder, populated by 06b
  - Update `InterviewSession`, `InterviewSessionState`, `CosmosSessionDocument`, `InterviewContractsMapping`, and all affected tests
- Compute `SessionResult` in `SubmitAnswer` from in-memory turns after each answer; persist on session via `SaveAnswerAsync` (no extra DB read)
- `SessionResult` is null when a session has 0 answered turns
- `aggregateScore int?` on `InterviewResponse`: null for `Created`/`Active`; null for `Completed` with 0 evaluated turns; otherwise `SessionResult.OverallScore`

### Detail read

- Add a dedicated `GET /api/interviews/{id}/details` endpoint (existing shallow endpoint is unchanged)
- Load the session and its ordered turns through the interview store, scoped to the authenticated user's partition
- Scope all reads to the authenticated user's partition
- Return the `summary` field when present (populated by 06b)
- Add response DTOs for the detailed view
- Add unit tests for query mapping and ordering
- Add integration tests for happy path and authorization

## Out of Scope

- Summary generation (06b) — this feature returns the summary field but does not produce it
- UI (06c)
- Dashboard aggregation (M07)
- Admin cross-user access

## Acceptance Criteria

### Domain and storage

- [x] `InterviewFeedback` is removed; `InterviewSession` has `SessionResult?`, `InterviewSummary?`, and `AiCallMetadata? SummaryMetadata` as separate properties
- [x] `CosmosSessionDocument` persists `result`, `summary`, and `summaryAi` as separate nullable fields
- [x] `SubmitAnswer` computes `SessionResult` from in-memory turns after every answer and saves it on the session
- [x] `SessionResult` is null when a session has 0 answered turns
- [x] `aggregateScore int?` is included in `InterviewResponse`: null for `Created`/`Active`; null for `Completed` with 0 evaluated turns; otherwise `SessionResult.OverallScore`

### Detail read

- [x] `GET /api/interviews/{id}/details` returns full ordered turn history for any session
- [x] Each turn includes question, answer, per-dimension scores, and feedback
- [x] Per-dimension scores are read from stored M05 output (no AI re-call)
- [x] The response includes `summary` when present
- [x] Turns are returned in correct order by turn number
- [x] Reads are scoped to the authenticated user; users cannot read others' sessions
- [x] Anonymous requests return `401`; non-invited authenticated requests return `403`
- [x] The detail read uses the existing interview store/query path, not repeated point reads for each turn
- [x] Approach documented: dedicated `GET /api/interviews/{id}/details`; shallow endpoint gains `aggregateScore int?`
- [x] Unit tests cover query mapping and ordering
- [x] Integration tests cover happy path and authorization
- [x] Existing tests continue to pass

## Tasks

### [x] Domain and storage refactor (do first)

- [x] Refactor `InterviewFeedback` → `SessionResult` + `InterviewSummary` + `SummaryMetadata` across domain, state, Cosmos, and contract layers
- [x] Add `SessionResult` computation to `SubmitAnswer` (use in-memory turns, no extra DB read)
- [x] Add `aggregateScore int?` to `InterviewResponse` and its mapping

### [x] Detail read implementation

- [x] Define detailed session/turn response DTOs
- [x] Load the session and ordered turns through the existing interview store/query path
- [x] Implement `GET /api/interviews/{id}/details`
- [x] Include `summary` (text + createdAt) in detail response when present

### [x] Tests

- [x] Unit tests for `SessionResult` computation and `aggregateScore` mapping
- [x] Unit tests for detail response mapping and ordering
- [x] Integration tests for detail read happy path and authorization

## Verification

- [x] Requesting a completed session returns all turns with scores and feedback
- [x] Turns are correctly ordered
- [x] No AI call is made when reading detail
- [x] Summary field is present when a summary exists
- [x] User cannot read another user's session
- [x] Anonymous → `401`, non-invited → `403`
- [x] Full test suite passes

## Dependencies and Blockers

Depends on:

- 04a - Interview API (turn persistence) ✅
- 05c - Rubric-based answer evaluation (stored per-dimension scores) ✅

Blocks:

- 06b - Session summary generation (uses the session detail read path; domain types introduced here)
- 06c - History and session detail UI

## Risks and Open Questions

### Risks

- Sessions with many turns could return large payloads; consider whether the detail read needs any bounding (likely fine for MVP session lengths).

### Open Questions

Decided: `GET /api/interviews/{id}/details` is a dedicated endpoint. The existing `GET /api/interviews/{id}` gains `aggregateScore int?` only. Two endpoints, two purposes.

## Notes

This is the read counterpart to M04's write path. It uses the existing interview store/query path with user-partition scoping and turn ordering, rather than issuing repeated point reads for individual turns.
