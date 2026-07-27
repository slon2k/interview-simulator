# 07a - Dashboard analytics API

Phase: 2  
Milestone: 07 - Dashboard analytics  
Type: Feature  
Status: Planned

## Summary

Add a dashboard summary endpoint that returns the basic progress metric set for the authenticated user, computed by aggregating stored session data. No AI calls.

## Problem and User Value

Users benefit from seeing progress across interviews: how their average score trends, which topics/types are strongest, and which rubric dimensions are consistently weakest. All of this is derivable from data M04–M06 already persist.

## Scope

- Add a single dashboard summary endpoint returning the basic metric set:
  - total completed sessions
  - average score
  - average score over time (recent trend)
  - scores by topic / interview type
  - weakest rubric dimensions
  - recent sessions
- Compute metrics with dedicated Cosmos aggregation query classes, partition-scoped to the user
- Reuse the read-query pattern (query classes in Infrastructure behind Features interfaces), not the point-read repository
- Define response DTOs for the dashboard payload
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
- [ ] Average score over time (recent trend) is returned
- [ ] Scores by topic / interview type are returned
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

- [ ] Define dashboard response DTOs
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

### Open Questions

- Compute all metrics server-side in one query pass, or fetch a bounded recent set and aggregate in the service? Default assumption: partition-scoped queries where cheap; bounded-set aggregation where not — documented with RU implications.
- Trend window size (e.g. last N sessions) — confirm during implementation.

## Notes

This feature is the analytics read model. It relies entirely on M05 having stored per-dimension scores, which is why "weakest rubric dimensions" needs no reprocessing.
