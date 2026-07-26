# 04c - Active interview UI

Phase: 2  
Milestone: 04 - Text interview flow  
Type: Feature  
Status: Planned

## Summary

Implement the active interview page at `/interviews/{id}`.

The page displays the current interview state, shows the current question, accepts a text answer, submits the answer to the backend, and advances to the next question. It also handles completion, including final-answer completion and early completion.

This feature provides the main text interview user experience for M04.

## Problem and User Value

04a provides the backend interview API, and 04b provides the list/setup entry points. This feature implements the core interview-taking experience.

Users should be able to:

- Open an active interview
- See the current question
- Type and submit an answer
- Move through questions one at a time
- Complete the interview
- Refresh or return later and resume the same current question

## Scope

- Create `/interviews/{id}` route
- Fetch interview state from `GET /api/interviews/{id}` on page load
- Display interview metadata:
  - role
  - seniority
  - topic
  - interview type
  - status
  - progress
- Display the current question for active interviews
- Display a text area for answer input
- Submit answers to `POST /api/interviews/{id}/answers`
- Include the current `turnNumber` when submitting answers
- Update the UI with the next question after successful answer submission
- Show completed state when the interview is completed
- Support early completion using `POST /api/interviews/{id}/complete`
- Handle loading states:
  - initial load
  - answer submission
  - early completion
- Handle API and network errors
- Prevent empty/whitespace answer submission
- Prevent accidental double-submit in the UI while request is pending
- Protect route using existing auth/access UI behavior

## Out of Scope

- Answer evaluation or feedback display
- Rubric display
- Full session history or previous answer review
- Final interview summary
- AI-generated follow-up questions
- Voice input/output
- Advanced completed interview review UI
- Dashboard/progress analytics

## Acceptance Criteria

- [ ] `/interviews/{id}` route exists
- [ ] Page loads interview state from `GET /api/interviews/{id}`
- [ ] Page displays interview metadata:
  - role
  - seniority
  - topic
  - interview type
  - status
  - progress
- [ ] Active interview displays the current question from API response
- [ ] Active interview displays a text area for answer input
- [ ] Answer submission requires non-empty text
- [ ] Answer submission posts to `POST /api/interviews/{id}/answers`
- [ ] Answer submission includes:
  - current `turnNumber`
  - answer text
- [ ] Submit button is disabled or guarded while answer submission is in progress
- [ ] Successful answer submission updates the page with the next current question when questions remain
- [ ] Successful final answer submission shows completed state
- [ ] Progress indicator updates after successful answer submission
- [ ] Early completion action calls `POST /api/interviews/{id}/complete`
- [ ] Successful early completion shows completed state
- [ ] Completed state shows a completion message and link back to `/interviews`
- [ ] Completed/read-only interviews can be opened from `/interviews/{id}`
- [ ] Completed/read-only state does not show answer submission UI
- [ ] Refreshing the page reloads the same persisted current question
- [ ] Initial loading state is shown while fetching interview state
- [ ] Error state is shown if loading interview state fails
- [ ] Error state is shown if answer submission fails
- [ ] Error state is shown if early completion fails
- [ ] User can retry after recoverable API/network errors
- [ ] Anonymous users are redirected to `/login`
- [ ] Non-invited authenticated users see access denied
- [ ] Existing tests continue to pass

## Tasks

### [ ] Page and routing

- [ ] Create `/interviews/{id}` route
- [ ] Add active interview page component
- [ ] Fetch interview state on page load
- [ ] Display interview metadata and progress
- [ ] Display current question for active interviews
- [ ] Display completed/read-only state for completed interviews
- [ ] Add link back to `/interviews`

### [ ] Answer flow

- [ ] Add text answer input
- [ ] Add client-side validation for empty/whitespace answers
- [ ] Submit `{ turnNumber, text }` to `POST /api/interviews/{id}/answers`
- [ ] Disable/guard submit while request is pending
- [ ] Update displayed question after successful submission
- [ ] Clear answer input after successful submission
- [ ] Show completed state after final answer

### [ ] Completion flow

- [ ] Add early completion action
- [ ] Call `POST /api/interviews/{id}/complete`
- [ ] Show completed state after successful completion
- [ ] Hide answer input/actions for completed interviews

### [ ] Loading, error, and access states

- [ ] Add initial loading state
- [ ] Add answer submission loading state
- [ ] Add completion loading state
- [ ] Add load error state
- [ ] Add answer submission error state
- [ ] Add completion error state
- [ ] Preserve typed answer after failed submission
- [ ] Protect route with existing auth/access behavior

### [ ] Tests

- [ ] Add UI/component test for active interview rendering if frontend test coverage exists
- [ ] Add UI/component test for answer validation if frontend test coverage exists
- [ ] Add UI/component test for answer submission/update flow if frontend test coverage exists
- [ ] Add UI/component test for completed state if frontend test coverage exists
- [ ] Otherwise verify manually and cover full flow in 04d

## Verification

### Manual/UI smoke verification

Using an invited authenticated user with an active interview:

- [ ] Navigating to `/interviews/{id}` loads without errors
- [ ] Page displays role, topic, seniority, interview type, status, and progress
- [ ] Current question is displayed
- [ ] Empty answer submission is blocked with validation message
- [ ] Submitting a valid answer calls `POST /api/interviews/{id}/answers`
- [ ] Answer request includes the current `turnNumber`
- [ ] Submit button is disabled or guarded while the request is pending
- [ ] After successful submission, the next question is displayed
- [ ] Progress indicator increments after successful submission
- [ ] Answer text area is cleared after successful submission
- [ ] Refreshing the page shows the same persisted current question
- [ ] Failed answer submission displays an error and keeps the typed answer
- [ ] Final answer shows completed state
- [ ] Completed state includes link back to `/interviews`

Using an invited authenticated user with a completed interview:

- [ ] Navigating to `/interviews/{id}` loads completed/read-only state
- [ ] Completed state does not show answer submission UI
- [ ] Completed state does not require full history/review in M04

Early completion:

- [ ] Clicking complete/finish action calls `POST /api/interviews/{id}/complete`
- [ ] Successful completion shows completed state
- [ ] Completion API failure displays an error

### Access verification

- [ ] Anonymous user navigating to `/interviews/{id}` is redirected to `/login`
- [ ] Non-invited authenticated user sees access denied
- [ ] User cannot view another user’s interview, based on 04a API behavior

### Regression verification

- [ ] Existing frontend behavior still works
- [ ] Existing tests continue to pass
- [ ] Full M04 end-to-end flow is covered later in 04d

## Dependencies and Blockers

Depends on:

- 04a - Interview API
- Existing authentication UI behavior
- Existing invite/access handling

Related to:

- 04b - Interview list and setup UI

Blocks:

- 04d - End-to-end text flow verification and documentation
- 05 - AI prompts and rubric evaluation UI integration

## Risks and Decisions

Risks:

- The page depends on the `GET /api/interviews/{id}` response shape from 04a.
- The page depends on the answer submission response returning updated current interview state.
- Completed interview rendering is intentionally minimal in M04 and may need expansion in M06.

Decisions:

- Primary action should be treated as “Submit answer”, not purely “Next question”.
- The backend decides whether the next state is another question or completed.
- The UI sends the current `turnNumber` with the answer to help reject stale or duplicate submissions.
- `/interviews/{id}` supports basic completed/read-only state in M04.
- Full previous question/answer history remains out of scope until M06.
- Full end-to-end browser flow is verified in 04d, not required as a separate sub-issue here.

## Notes

Route:

```text
/interviews/{id}
```

API calls used by this feature:

```http
GET  /api/interviews/{id}
POST /api/interviews/{id}/answers
POST /api/interviews/{id}/complete
```

Suggested answer request body:

```json
{
  "turnNumber": 1,
  "text": "My answer..."
}
```

Expected active interview state includes a current question:

```json
{
  "id": "0197f846-fb4c-7d5e-a1aa-000000000000",
  "status": "active",
  "questionCount": 3,
  "answeredCount": 1,
  "currentQuestion": {
    "turnNumber": 2,
    "text": "Describe a challenging dotnet problem you solved."
  }
}
```

Expected completed interview state has no current question:

```json
{
  "id": "0197f846-fb4c-7d5e-a1aa-000000000000",
  "status": "completed",
  "questionCount": 3,
  "answeredCount": 3,
  "currentQuestion": null
}
```
