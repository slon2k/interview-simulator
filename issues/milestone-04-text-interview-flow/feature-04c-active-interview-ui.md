# 04c - Active interview UI

Phase: 2
Milestone: 04 - Text interview flow
Type: Feature
Status: Planned

## Summary

Implement active interview page at `/interviews/{id}`. Displays current question, text answer input, and turn progression. Handles answer submission, next question fetch, and completion flow.

## Problem and User Value

The core user experience: show question, accept text answer, advance to next question, complete when done. This feature is the heart of the MVP.

## Scope

- Create `/interviews/{id}` route in React router
- Fetch interview state from `GET /api/interviews/{id}` on mount
- Display interview metadata (role, seniority, topic, type, progress)
- Display current question (from active turn or first turn if not started)
- Text area for answer input
- "Next question" button → POST `/api/interviews/{id}/answers` → fetch new question
- "Complete interview" button → POST `/api/interviews/{id}/complete` → show completion message
- Handle loading states (question fetch, answer submission, completion)
- Handle errors (API failures, network issues)
- Show progress (e.g., "Question 1 of 5")
- Protect route: redirect anonymous users to `/login`, non-invited to access denied
- Test happy path: load interview → answer 3 questions → complete

## Out of Scope

- Answer evaluation or feedback display
- Rubric display
- Session history or past answers (M06)
- AI-generated follow-up questions (M05)
- Voice input (future)

## Acceptance Criteria

- [ ] `/interviews/{id}` route exists and loads interview state
- [ ] Current question displays from API response
- [ ] Text area for answer input
- [ ] "Next question" button POSTs answer and updates display with new question
- [ ] "Complete interview" button POSTs completion request
- [ ] After completion: show completion message, offer link to `/interviews` (list page)
- [ ] Progress indicator shows "Question N of M"
- [ ] Loading state during API calls
- [ ] Error display on API failure with retry option
- [ ] Anonymous users redirected to `/login`
- [ ] Non-invited users see access denied
- [ ] Form prevents empty answer submission
- [ ] Happy path E2E test: load → answer 3 questions → complete → return to list

## Sub-Issues

- [ ] Task: Create active interview page component
- [ ] Task: Fetch interview state on mount (GET /api/interviews/{id})
- [ ] Task: Display question and metadata
- [ ] Task: Implement answer input and next question flow
- [ ] Task: Implement complete interview flow
- [ ] Task: Add loading and error states
- [ ] Task: Add progress indicator
- [ ] Task: Protect route with auth guard
- [ ] Task: E2E test for happy path

## Verification

- [ ] Page renders interview state without errors
- [ ] Question displays correctly
- [ ] Answer submission updates question
- [ ] Progress counter increments
- [ ] Completion shows success message
- [ ] Return to list link works
- [ ] Anonymous users cannot access page
- [ ] Error message displays on API failure
