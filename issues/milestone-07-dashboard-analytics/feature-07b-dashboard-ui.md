# 07b - Dashboard UI

Phase: 2  
Milestone: 07 - Dashboard analytics  
Type: Feature  
Status: Planned

## Summary

Replace the existing `/dashboard` placeholder page with a dashboard that renders the basic progress metrics from the 07a summary endpoint: average score, score trend, scores by focus area and interview type, weakest rubric dimensions, and recent sessions.

## Problem and User Value

07a exposes the metric set via the API. This feature makes it visible: a single dashboard page giving the user an at-a-glance view of their progress and what to work on next.

## Scope

- Dashboard page consuming the 07a summary endpoint
- Render:
  - total completed sessions and average score (headline stats)
  - average score over time (line chart for the last 5 completed sessions)
  - scores by focus area (bar chart)
  - scores by interview type (bar chart)
  - weakest rubric dimensions
  - recent sessions (with links to detail from M06)
- Loading and error states consistent with existing pages
- Empty state for users with no completed sessions yet
- Gate the page to authenticated invited users, consistent with existing routing

## Out of Scope

- Analytics API (07a)
- Advanced/interactive analytics — stretch/future
- Voice (M08)

## Acceptance Criteria

- [ ] A dashboard page renders the basic metric set from the 07a endpoint
- [ ] Headline stats (total completed, average score) are shown
- [ ] Score trend over time is shown
- [ ] Scores by focus area and interview type are shown separately
- [ ] Weakest rubric dimensions are shown
- [ ] Recent sessions are shown and link to their detail pages
- [ ] Empty state renders for users with no completed sessions
- [ ] Loading and error states are handled consistently
- [ ] Page is restricted to authenticated invited users
- [ ] Existing tests continue to pass

## Tasks

### [ ] Dashboard page

- [ ] Replace the existing dashboard placeholder page
- [ ] Fetch the summary endpoint
- [ ] Render headline stats
- [ ] Render trend chart
- [ ] Render scores by focus area and interview type
- [ ] Render weakest dimensions
- [ ] Render recent sessions with detail links
- [ ] Empty, loading, and error states

## Verification

- [ ] Dashboard shows correct metrics for a user with completed sessions
- [ ] Trend, focus-area/interview-type breakdowns, and weakest dimensions render
- [ ] Recent sessions link to detail
- [ ] Empty state renders for a new user
- [ ] Loading and error states behave correctly
- [ ] Unauthenticated/non-invited users cannot reach the page
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 07a - Dashboard analytics API
- 06c - History and session detail UI (recent-session detail links)

Blocks:

- 07c - End-to-end verification and Phase 2 exit

## Risks and Open Questions

### Risks

- Mantine Charts is already installed (`@mantine/charts` with Recharts); use it for the basic line and bar charts.

### Open Questions

- Charting approach is resolved: use `@mantine/charts`.

## Notes

The dashboard is read-only and consumes a single endpoint. It performs no aggregation of its own beyond presentation.
