# 07a - Dashboard analytics API

Phase: 2  
Milestone: 07 - Dashboard analytics  
Type: Feature  
Status: Planned

## Summary

Add a dashboard summary endpoint that returns the basic progress metric set for the authenticated user, computed by aggregating stored session and turn data. No AI calls.

## Problem and User Value

Users benefit from seeing progress across interviews: how their average score trends, which topics/types are strongest, and which rubric dimensions are consistently weakest. All of this is derivable from data M04–M06 already persist.

## Scope

- Add a single dashboard summary endpoint returning the basic metric set:
  - total completed sessions
  - average score
  - average score over time (last 5 completed scored sessions, oldest to newest)
  - scores by focus area
  - scores by interview type
  - weakest rubric dimensions
  - recent sessions
- Compute metrics with dedicated Cosmos aggregation query classes, partition-scoped to the user
- Reuse the read-query pattern (query classes in Infrastructure behind Features interfaces), not the point-read repository
- Define response DTOs for the dashboard payload
- Use whole-number scores on the existing 0-100 scale; averages are rounded for presentation
- Return the 5 most recent completed sessions for quick review links
- Ensure analytics cover only the authenticated user's sessions
- Require invited-user authorization
- Add unit tests for aggregation logic
- Add integration tests for happy path and authorization

## Out of Scope

- Any AI calls
- Dashboard UI (07b)
- Advanced analytics (learning plans, long-term trends) — stretch/future
- Cross-user or admin analytics

## Acceptance Criteria

- [ ] A dashboard summary endpoint exists
- [ ] Total completed sessions is returned
- [ ] Average score is returned
- [ ] Average score over time for the last 5 completed scored sessions is returned
- [ ] Scores by focus area and interview type are returned separately
- [ ] Weakest rubric dimensions are returned
- [ ] Recent sessions are returned
- [ ] All metrics are computed from stored data with no AI calls
- [ ] Aggregation uses query classes, partition-scoped to the user
- [ ] Analytics cover only the authenticated user's data
- [ ] Anonymous → `401`, non-invited → `403`
- [ ] Unit tests cover aggregation logic
- [ ] Integration tests cover happy path and authorization
- [ ] Existing tests continue to pass

## Tasks

### [ ] Aggregation queries

- [ ] Define dashboard response DTOs using the M07 response shape
- [ ] Add aggregation query class(es) for the metric set
- [ ] Register queries in `Startup/Persistence.cs`

### [ ] Endpoint

- [ ] Implement the dashboard summary endpoint
- [ ] Enforce invited-user authorization and user scoping

### [ ] Tests

- [ ] Unit tests for aggregation logic
- [ ] Integration tests for happy path and authorization

## Verification

- [ ] The endpoint returns the full basic metric set for the user
- [ ] Metrics reflect the user's stored sessions
- [ ] No AI call is made
- [ ] User cannot see another user's analytics
- [ ] Anonymous → `401`, non-invited → `403`
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 05b - Rubric-based answer evaluation (stored per-dimension scores)
- 06 - Session history and summaries (completed sessions to aggregate)

Blocks:

- 07b - Dashboard UI
- 07c - End-to-end verification and Phase 2 exit

## Risks and Open Questions

### Risks

- Some aggregates may be awkward or costly to express as single Cosmos queries; a bounded recent-sessions computation may be needed for MVP.

### Decisions

- Use dedicated partition-scoped projection queries rather than loading full domain objects or aggregating in the frontend. Session metrics are aggregated in the application service from the completed-session projection.
- Compute weakest dimensions from a narrow projection of the authenticated user's evaluated turn documents. Filter those rows in application code using the IDs of completed sessions with stored scores. This avoids large dynamic `IN` clauses and is acceptable for the MVP because interview volumes are expected to remain small.
- Exclude evaluated turns from active or incomplete sessions before grouping weakest dimensions. Add integration coverage for this filtering behavior.
- Trend window is resolved to the last 5 completed scored sessions, ordered oldest to newest.

## Notes

This feature is the analytics read model. It relies entirely on M05 having stored per-dimension scores, which is why "weakest rubric dimensions" needs no reprocessing. The session projection should select only the fields needed for dashboard metrics, including `result.totalScore`; the turn projection should select only `sessionId` and evaluation dimension fields. If session volume grows significantly, revisit the application-side filtering in favor of a more selective server-side query or a dedicated analytics read model, and document the resulting RU implications.
