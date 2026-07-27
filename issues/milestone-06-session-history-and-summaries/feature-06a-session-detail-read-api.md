# 06a - Session detail read API

Phase: 2  
Milestone: 06 - Session history and summaries  
Type: Feature  
Status: Planned

## Summary

Add the read path that returns a completed interview in full — setup metadata plus every turn with its question, answer, per-dimension scores, and feedback — so the history and detail UI can render it. M04 deliberately kept the detail endpoint shallow; this feature completes it.

## Problem and User Value

M04 stored full turn history but `GET /api/interviews/{id}` only returned enough state to resume an active interview, not the full answer/score/feedback history. Users cannot yet review a finished interview. This feature exposes the stored history for review.

## Scope

- Return full turn history for a session:
  - question, answer, per-dimension scores, feedback, prompt versions
- Load session + ordered turns via a dedicated query class following the `ISessionHistoryQueries` pattern (not the point-read `IRepository<T>`)
- Scope all reads to the authenticated user's partition
- Return the session summary field (populated by 06b) if present
- Decide and document whether detail is served by extending `GET /api/interviews/{id}` or a dedicated detail endpoint
- Add response DTOs for the detailed view
- Add unit tests for query mapping and ordering
- Add integration tests for happy path and authorization

## Out of Scope

- Summary generation (06b) — this feature returns the summary field but does not produce it
- UI (06c)
- Dashboard aggregation (M07)
- Admin cross-user access

## Acceptance Criteria

- [ ] A detail read returns full ordered turn history for a session
- [ ] Each turn includes question, answer, per-dimension scores, and feedback
- [ ] Per-dimension scores are read from stored M05 output (no AI re-call)
- [ ] The response includes the persisted summary when present
- [ ] Turns are returned in correct order (by turn number)
- [ ] Reads are scoped to the authenticated user; users cannot read others' sessions
- [ ] Anonymous requests return `401`; non-invited authenticated requests return `403`
- [ ] The detail read uses a query class, not the point-read repository
- [ ] Approach (extend `{id}` vs dedicated endpoint) is documented
- [ ] Unit tests cover query mapping and ordering
- [ ] Integration tests cover happy path and authorization
- [ ] Existing tests continue to pass

## Tasks

### [ ] Detail read implementation

- [ ] Define detailed session/turn response DTOs
- [ ] Add session detail query (session + ordered turns)
- [ ] Wire query registration in `Startup/Persistence.cs`
- [ ] Implement detail endpoint / extend existing endpoint
- [ ] Include summary field in response

### [ ] Tests

- [ ] Unit tests for mapping and ordering
- [ ] Integration tests for happy path and authorization

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
- 05b - Rubric-based answer evaluation (stored per-dimension scores)

Blocks:

- 06b - Session summary generation (shares detail read for summary input)
- 06c - History and session detail UI

## Risks and Open Questions

### Risks

- Sessions with many turns could return large payloads; consider whether the detail read needs any bounding (likely fine for MVP session lengths).

### Open Questions

- Extend `GET /api/interviews/{id}` with a detail projection, or add `GET /api/interviews/{id}/detail`? Default assumption: a detail projection on the existing resource, documented in the API notes.

## Notes

This is the read counterpart to M04's write path. It reuses the query-class approach discussed for Cosmos reads — filters and projections live in a dedicated class, partition-scoped to the user, rather than being bolted onto the generic repository.
