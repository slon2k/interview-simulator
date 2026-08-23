# Milestone 07 - Dashboard analytics

Epic type: Milestone

## Overview

Provide a progress dashboard computed from stored interview sessions. This is a pure read/aggregation milestone - no AI calls. Every metric is derived from the per-dimension scores and session metadata already persisted in M04-M06.

This is the final milestone of Phase 2 (Text Interview MVP). Completing it means an invited user can run text interviews, receive structured feedback, review history and summaries, and see progress over time — without voice.

## Feature Issues

- 07a - Dashboard analytics API (aggregation queries)
- 07b - Dashboard UI
- 07c - End-to-end verification, documentation, and Phase 2 exit

## Key Decisions

- **No AI**: all metrics are aggregations over stored data. M05 deliberately persisted per-dimension scores so analytics never needs to re-call the model.
- **Aggregation via query classes**: metrics are computed with dedicated Cosmos query classes (partition-scoped to the user), following the read-query pattern used since M04 — not the point-read repository, and not client-side aggregation of full session payloads where a query can do it.
- **One summary endpoint**: a single dashboard endpoint returns the metric set the UI needs, rather than many chatty per-metric endpoints.
- **User-scoped**: analytics only ever cover the authenticated user's own sessions. No cross-user aggregation in Phase 2.
- **Basic scope only**: the metric set matches the roadmap's "Basic Dashboard Scope". Advanced analytics are explicitly a stretch/future item.
- **Existing dashboard route**: `/dashboard` and its navigation entry already exist; 07b replaces the current placeholder page.
- **Bounded data**: the score trend contains the last 5 completed scored sessions, ordered oldest to newest, and recent sessions contains the 5 most recent completed sessions.
- **Whole-number scores**: scores use the existing 0-100 scale and averages are rounded to whole numbers.
- **Separate breakdowns**: the UI renders scores by `focusArea` and interview type as separate charts.

## Metric Set (Basic Dashboard Scope)

- Total completed sessions
- Average score
- Average score over time (last 5 completed sessions)
- Scores by focus area
- Scores by interview type
- Scores by rubric dimension
- Recent sessions

## Dashboard Response Shape

The single dashboard endpoint returns this user-scoped read model:

- `totalCompletedSessions: number`
- `averageScore: number | null`
- `scoreTrend: { interviewId, completedAt, score }[]`
- `scoresByFocusArea: { focusArea, score, sessionCount }[]`
- `scoresByInterviewType: { interviewType, score, sessionCount }[]`
- `scoresByDimension: { key, label, score, sampleCount }[]`
- `recentSessions: { id, targetRole, focusArea, interviewType, completedAt, totalScore }[]`

Aggregates exclude incomplete sessions and sessions without a stored total score. Empty datasets are represented by empty arrays; `averageScore` is `null` when there are no completed scored sessions.

## Exit Criteria

- All 3 features shipped and merged
- A dashboard summary endpoint returns the basic metric set
- The dashboard UI renders average score, trend, separate focus-area, interview-type, and rubric-dimension charts, plus recent sessions
- All metrics are computed from stored data with no AI calls
- Analytics are user-scoped
- All existing tests pass; new unit and integration tests added
- Architecture documentation updated
- **Phase 2 exit criteria met** (see `docs/roadmap.md`)

## Notes

Because M05 stored per-dimension scores (not just overall), "scores by rubric dimension" and "scores by focus area/interview type" are straightforward aggregations rather than requiring stored data to be reprocessed by the AI.

Cosmos aggregation note: partition-scoped aggregate queries over a user's sessions are the target. Where an aggregate cannot be expressed cheaply in a single query, computing from a bounded set of the user's recent sessions is acceptable for MVP — document the choice and its RU implications.
