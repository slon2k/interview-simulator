# 06c - History and session detail UI

Phase: 2  
Milestone: 06 - Session history and summaries  
Type: Feature  
Status: Partial

## Summary

Build the frontend pages that let users browse their completed interviews and review a single interview in full — questions, answers, per-dimension scores, feedback, and the AI summary.

Note: the basic interview list/detail routes are present, but the full review experience is not yet wired to the new detail payload and summary UI.

## Problem and User Value

M06a/06b expose review data and summaries via the API. This feature makes them usable: a history list to find a past interview and a detail page to read through it. This completes the review half of the text interview MVP.

## Scope

- History page listing the user's completed interviews with key metadata (topic, role, type, date, overall score)
- Optional filtering/sorting consistent with the existing `/interviews` list conventions
- Session detail page rendering:
  - session summary
  - each turn's question, answer, per-dimension scores, feedback
- Navigation from history → detail
- Loading and error states consistent with existing pages
- Handle the not-yet-summarized case gracefully (session viewable without a summary)
- Ensure pages are gated to authenticated invited users, consistent with existing routing

## Out of Scope

- Detail/summary API (06a, 06b)
- Dashboard analytics UI (M07)
- Voice (M08)
- Editing past sessions

## Acceptance Criteria

- [ ] A history page lists the user's completed interviews
- [ ] History entries show key metadata and link to detail
- [ ] A session detail page renders the summary and full turn history
- [ ] Per-dimension scores and feedback are shown per turn
- [ ] A session without a summary still renders (graceful empty/pending state)
- [ ] Loading and error states are handled consistently with existing pages
- [ ] Pages are restricted to authenticated invited users
- [ ] History and detail read only from the user's own data
- [ ] Existing tests continue to pass

## Tasks

### [ ] History page

- [ ] Add history route/page
- [ ] Fetch and render completed interviews
- [ ] Link entries to detail
- [ ] Loading/error states

### [ ] Session detail page

- [ ] Add detail route/page
- [ ] Render summary
- [ ] Render ordered turns with scores and feedback
- [ ] Handle missing-summary state
- [ ] Loading/error states

## Verification

- [ ] History page shows the user's completed interviews
- [ ] Clicking an entry opens its detail page
- [ ] Detail page shows summary + all turns with scores and feedback
- [ ] A session without a summary still renders
- [ ] Loading and error states behave correctly
- [ ] Unauthenticated/non-invited users cannot reach the pages
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 06a - Session detail read API
- 06b - Session summary generation

Blocks:

- 06d - End-to-end verification and documentation

## Risks and Open Questions

### Risks

- Rendering many turns with rich feedback could get visually heavy; may need collapsing/pagination for long sessions.

### Open Questions

- Reuse the existing `/interviews` list for completed sessions with a status filter, or a dedicated `/history` route? Default assumption: a dedicated history view for completed sessions, reusing shared list components. Confirm with routing conventions in M04.

## Notes

The detail page is read-only. It consumes 06a's detail payload and 06b's summary; it performs no scoring or AI calls of its own.
