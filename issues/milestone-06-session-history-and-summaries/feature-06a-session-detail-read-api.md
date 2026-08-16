# 06a - Session detail read API

Phase: 2  
Milestone: 06 - Session history and summaries  
Type: Feature  
Status: Planned

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

- Add a dedicated `GET /api/interviews/{id}/detail` endpoint (existing shallow endpoint is unchanged)
- Load session + ordered turns via a dedicated query class (not the point-read `IRepository<T>`)
- Scope all reads to the authenticated user's partition
- Return `summary` and `summaryAi` fields when present (populated by 06b)
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

- [ ] `InterviewFeedback` is removed; `InterviewSession` has `SessionResult?`, `InterviewSummary?`, and `AiCallMetadata? SummaryMetadata` as separate properties
- [ ] `CosmosSessionDocument` persists `result`, `summary`, and `summaryAi` as separate nullable fields
- [ ] `SubmitAnswer` computes `SessionResult` from in-memory turns after every answer and saves it on the session
- [ ] `SessionResult` is null when a session has 0 answered turns
- [ ] `aggregateScore int?` is included in `InterviewResponse`: null for `Created`/`Active`; null for `Completed` with 0 evaluated turns; otherwise `SessionResult.OverallScore`

### Detail read

- [ ] `GET /api/interviews/{id}/detail` returns full ordered turn history for any session
- [ ] Each turn includes question, answer, per-dimension scores, and feedback
- [ ] Per-dimension scores are read from stored M05 output (no AI re-call)
- [ ] The response includes `summary` when present
- [ ] Turns are returned in correct order by turn number
- [ ] Reads are scoped to the authenticated user; users cannot read others' sessions
- [ ] Anonymous requests return `401`; non-invited authenticated requests return `403`
- [ ] The detail read uses a query class, not the point-read repository
- [x] Approach documented: dedicated `GET /api/interviews/{id}/detail`; shallow endpoint gains `aggregateScore int?`
- [ ] Unit tests cover query mapping and ordering
- [ ] Integration tests cover happy path and authorization
- [ ] Existing tests continue to pass

## Tasks

### [ ] Domain and storage refactor (do first)

- [ ] Refactor `InterviewFeedback` → `SessionResult` + `InterviewSummary` + `SummaryMetadata` across domain, state, Cosmos, and contract layers
- [ ] Add `SessionResult` computation to `SubmitAnswer` (use in-memory turns, no extra DB read)
- [ ] Add `aggregateScore int?` to `InterviewResponse` and its mapping

### [ ] Detail read implementation

- [ ] Define detailed session/turn response DTOs
- [ ] Add session detail query (session + ordered turns, ordered by turn number)
- [ ] Wire query registration in `Startup/InterviewServices.cs`
- [ ] Implement `GET /api/interviews/{id}/detail`
- [ ] Include `summary` (text + createdAt) in detail response when present

### [ ] Tests

- [ ] Unit tests for `SessionResult` computation and `aggregateScore` mapping
- [ ] Unit tests for detail response mapping and ordering
- [ ] Integration tests for detail read happy path and authorization

## Verification

- [ ] Requesting a completed session returns all turns with scores and feedback
- [ ] Turns are correctly ordered
- [ ] No AI call is made when reading detail
- [ ] Summary field is present when a summary exists
- [ ] User cannot read another user's session
- [ ] Anonymous → `401`, non-invited → `403`
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 04a - Interview API (turn persistence) ✅
- 05c - Rubric-based answer evaluation (stored per-dimension scores) ✅

Blocks:

- 06b - Session summary generation (uses the session detail query; domain types introduced here)
- 06c - History and session detail UI

## Risks and Open Questions

### Risks

- Sessions with many turns could return large payloads; consider whether the detail read needs any bounding (likely fine for MVP session lengths).

### Open Questions

Decided: `GET /api/interviews/{id}/detail` is a dedicated endpoint. The existing `GET /api/interviews/{id}` gains `aggregateScore int?` only. Two endpoints, two purposes.

## Notes

This is the read counterpart to M04's write path. It reuses the query-class approach discussed for Cosmos reads — filters and projections live in a dedicated class, partition-scoped to the user, rather than being bolted onto the generic repository.
