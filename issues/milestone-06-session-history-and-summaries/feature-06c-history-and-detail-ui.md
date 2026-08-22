# 06c - History filter and completed-interview detail UI

Phase: 2  
Milestone: 06 - Session history and summaries  
Type: Feature  
Status: Complete

## Summary

Let users find and review completed interviews using the frontend that already exists, rather than new pages or routes. The interview list (`/interviews`) gets a status filter instead of a separate history page. The interview detail page (`/interviews/{id}`) already branches by status (`Created` / `Active` / `Completed`); this feature replaces the `Completed` branch's placeholder with a real review view backed by the 06a `/details` endpoint and the 06b summary.

The list filter and completed-interview review experience are implemented using the existing routes and the 06a/06b API payloads.

## Problem and User Value

06a and 06b expose the full turn history and AI summary via the API. This feature makes that data available through the existing list and detail pages: users can filter interviews by status and review completed sessions with summary and turn-level evaluation feedback.

## Decisions

- **No new route.** `/interviews` gains a status filter; there is no separate `/history` page. This resolves the open question left in the original version of this feature.
- **No new detail page.** `InterviewDetailPage` already branches on `interview.status` into `CreatedInterview` / `ActiveInterview` / `CompletedInterview`. Only `CompletedInterview` changes. `CreatedInterview` and `ActiveInterview` are untouched — they continue to use the existing shallow `getInterview` query and do not need turn history or evaluations.
- **No visual redesign.** The list stays a table, not a card grid. Scope is limited to adding a filter and, if needed, showing status-appropriate columns (e.g. a score for completed rows). A broader visual pass is explicitly not part of this iteration.
- **A second, conditional query on the detail page.** `CompletedInterview` fetches `/interviews/{id}/details` via a new `getInterviewDetails` API function, gated with `enabled: status === 'Completed'`, separate from the page's existing top-level `getInterview` query. `Created`/`Active` never trigger this fetch.
- **Segmented control for the status filter.** The list filter uses a segmented control (compact, discoverable, fits the table layout) with options: All, Created, Active, Completed. Default is "All".
- **Expandable turn cards for feedback review.** Each completed interview's turn list renders as a card per turn (collapsed: question + overall score; expanded: answer + per-dimension evaluation with score, label, and feedback). This allows users to scan scores first, then drill into detail as needed.
- **Query string strategy for future pagination.** Query key includes an optional params object (`['interviews', { status, page, pageSize }]`) from the start, so that when pagination lands on the backend, no client-side refactoring is needed. Currently `page` and `pageSize` are accepted but ignored (always fetch all results).

## Scope

### List page (`InterviewListPage`)

- Add a segmented control for status filtering (options: All, Created, Active, Completed) that drives `getInterviews({ status: [...] })`.
- Query key becomes `['interviews', { status: filterValue }]` to support optional pagination params later (e.g. `{ status, page, pageSize }`).
- Default filter is "All", so existing behavior for users mid-interview does not regress.
- Confirm the existing completed-row action (`statusAction` → "View") already links to `/interviews/{id}`; no separate history link to wire.

### Detail page (`InterviewDetailPage` / `CompletedInterview`)

- Add `getInterviewDetails(interviewId)` to `interviewApi.ts`, typed from the regenerated OpenAPI contract (depends on 06a/06b's shape being final in the contract).
- Rewrite `CompletedInterview` to:
  - Fetch `/details` via a conditional `useQuery` (`enabled` on `Completed` status).
  - Render its own loading/error states for this fetch, consistent with the rest of the page.
  - Render the AI summary text, with a graceful fallback: "Summary pending..." when no summary exists yet (06b's summary generation is best-effort and can be pending/failed).
  - Render the ordered turn list as **expandable cards**: each card shows collapsed (question + overall score); expanded to show answer + per-dimension evaluation (score, label, feedback per dimension). This allows users to scan overall scores first, then expand for detailed feedback.
- `CreatedInterview` and `ActiveInterview` receive no changes.

## Out of Scope

- Dashboard analytics UI (M07).
- Voice (M08).
- Editing past sessions.
- Visual/layout redesign of the list (cards, etc.) - explicitly deferred.
- Any change to the live interview flow (`Created`/`Active` branches, the answer-submission form, the shallow `getInterview` query).
- A dedicated `/history` route (superseded by the filter decision above).

## Acceptance Criteria

- [x] `/interviews` supports filtering by status; filtering to "Completed" shows only finished interviews
- [x] The default filter view is decided and does not hide in-progress interviews unexpectedly
- [x] A completed interview's "View" action opens `/interviews/{id}` and renders the completed-review UI
- [x] `CompletedInterview` fetches and renders the `/details` payload (summary + full turn history)
- [x] Each turn shows its question, answer, and per-dimension evaluation with the overall score
- [x] A completed interview without a summary yet renders gracefully ("Summary pending..." shown, no error, turns still visible)
- [x] `Created` and `Active` interview views are unchanged in behavior and network calls
- [x] The detail-fetch query only runs for `Completed` status (verified by frontend tests)
- [x] Loading and error states for the details fetch are handled consistently with existing page conventions
- [x] Pages remain restricted to authenticated invited users; detail/list data is scoped to the requesting user
- [x] Existing tests continue to pass; new tests cover the filter and the rewritten `CompletedInterview`

## Tasks

### List page filter

- [x] Add status filter control to `InterviewListPage`
- [x] Wire filter state into the `getInterviews` query key and call
- [x] Decide and implement default filter value
- [x] Confirm completed-row action links correctly (no change expected)

### API layer

- [x] Regenerate OpenAPI contract types (if not already current with 06a/06b)
- [x] Add `getInterviewDetails` to `interviewApi.ts`

### Detail page - completed branch

- [x] Add conditional `useQuery` for `/details` in `CompletedInterview`, gated on `Completed` status
- [x] Render summary with "Summary pending..." fallback when summary is absent or pending
- [x] Render turn list as expandable cards: collapsed shows question + overall score; expanded shows answer + per-dimension evaluation
- [x] Loading/error states for the details fetch (consistent with page conventions)

### Tests

- [x] List page: filter changes the rendered set and the query params
- [x] `CompletedInterview`: renders summary + turns from a mocked `/details` response
- [x] `CompletedInterview`: renders a graceful state when summary is absent
- [x] Confirm `Created`/`Active` branches do not call `getInterviewDetails`

## Verification

- [x] Filtering the list to "Completed" shows only finished interviews; other filters behave correctly
- [x] Opening a completed interview shows its summary and full turn-by-turn evaluation
- [x] Opening a created or active interview behaves exactly as before (no regression, no extra network call)
- [x] A completed interview with a pending/failed summary still renders its turns without error
- [x] Full test suite passes (`tsc`, `vitest`, lint)

## Dependencies and Blockers

Depends on:

- 06a - Session detail read API (`/interviews/{id}/details`)
- 06b - Session summary generation

Blocks:

- 06d - End-to-end verification and documentation

## Risks and Open Questions

### Risks

- Rendering many turns with rich per-dimension feedback could get visually heavy for long interviews. The expandable-card pattern mitigates this by allowing users to collapse turns, but turn-level or interview-level pagination may be needed for very long interviews; that is a follow-up, not required for this pass.

### Open Questions (resolved)

- ~~Reuse `/interviews` with a filter, or a dedicated `/history` route?~~ Resolved: reuse `/interviews` with a status filter. No new route.
- ~~Separate detail page for review vs. live flow?~~ Resolved: no new page. `InterviewDetailPage`'s existing status branch is extended; only the `Completed` branch changes.

## Notes

This feature is deliberately scoped to backend-data-consumption, not redesign: the table stays a table, the page stays one page with three status branches, and the only new network call is the conditional `/details` fetch that already has a home in the existing `CompletedInterview` component. All AI-provenance metadata (prompt versions, model, provider) stays out of this UI per the earlier decision not to expose AI metadata to end users.
