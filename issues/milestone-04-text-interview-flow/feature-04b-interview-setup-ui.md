# 04b - Interview list and setup UI

Phase: 2  
Milestone: 04 - Text interview flow  
Type: Feature  
Status: Planned

## Summary

Implement the `/interviews` list/resume page and `/interviews/new` setup form.

Users can view their interviews, resume active interviews, and start a new interview from a setup form. The setup form posts to the interview API created in 04a and navigates the user to the active interview page.

## Problem and User Value

04a provides the backend interview API. This feature provides the frontend entry point for the text interview flow.

Users need to be able to:

- See existing interviews
- Continue an active interview
- Start a new interview
- Configure basic interview settings before starting

This feature enables the UI flow needed before implementing the active interview experience in 04c.

## Scope

- Create `/interviews` list/resume page
- Fetch interviews from `GET /api/interviews`
- Display basic interview metadata:
  - role
  - topic
  - seniority
  - interview type
  - progress
  - status
- Show a continue/resume link for `active` interviews
- Show a basic view link for `completed` interviews
- Add a “New interview” button/link to `/interviews/new`
- Add basic list loading, error, and empty states
- Create `/interviews/new` route
- Build setup form with:
  - role
  - seniority
  - topic
  - interview type
  - question count
- Use hardcoded dropdown/select options for M04
- Validate required form fields
- Submit setup form to `POST /api/interviews`
- Navigate to `/interviews/{id}` after successful creation
- Display API errors and allow retry
- Add loading state while creating an interview
- Protect routes using existing auth/access UI behavior

## Out of Scope

- Active interview answer flow UI
- Full completed interview review/history
- Answer evaluation or feedback display
- Dashboard analytics
- Advanced filtering, sorting, or pagination
- Dynamic role/topic management
- Role/topic recommendations
- Saving incomplete setup form state
- Advanced UI polish

## Acceptance Criteria

- [ ] `/interviews` route exists
- [ ] `/interviews` calls `GET /api/interviews`
- [ ] List page displays current user’s interviews
- [ ] Each interview row/card shows:
  - role
  - topic
  - seniority
  - interview type
  - progress
  - status
- [ ] Active interviews show a continue/resume action linking to `/interviews/{id}`
- [ ] Completed interviews show a basic view action linking to `/interviews/{id}`
- [ ] List page has a “New interview” action linking to `/interviews/new`
- [ ] List page shows a loading state while interviews are loading
- [ ] List page shows an error state if loading interviews fails
- [ ] List page shows an empty state when the user has no interviews
- [ ] `/interviews/new` route exists
- [ ] Setup form renders all required fields:
  - role
  - seniority
  - topic
  - interview type
  - question count
- [ ] Setup form uses hardcoded M04 options
- [ ] Form validation prevents submission when required fields are missing
- [ ] Submit action posts form values to `POST /api/interviews`
- [ ] Submit button shows loading/disabled state while request is in progress
- [ ] On success, user is navigated to `/interviews/{id}`
- [ ] On API error, an error message is displayed and the form remains editable
- [ ] Cancel/back action returns to `/interviews`
- [ ] Anonymous users are redirected to `/login`
- [ ] Non-invited authenticated users see access denied
- [ ] Existing tests continue to pass

## Tasks

### [ ] List page

- [ ] Create `/interviews` route
- [ ] Add list/resume page component
- [ ] Fetch interviews from `GET /api/interviews`
- [ ] Display interview metadata and progress
- [ ] Add continue link for active interviews
- [ ] Add basic view link for completed interviews
- [ ] Add “New interview” link/button
- [ ] Add loading state
- [ ] Add error state
- [ ] Add empty state
- [ ] Protect route with existing auth/access behavior

### [ ] Setup page

- [ ] Create `/interviews/new` route
- [ ] Add setup form component
- [ ] Add hardcoded role/seniority/topic/interview type/question count options
- [ ] Add client-side required-field validation
- [ ] Implement `POST /api/interviews` call
- [ ] Navigate to `/interviews/{id}` on success
- [ ] Display API error messages
- [ ] Add submit loading/disabled state
- [ ] Add cancel/back link to `/interviews`
- [ ] Protect route with existing auth/access behavior

### [ ] Tests

- [ ] Add UI/component test for list loading/rendering if frontend test coverage exists
- [ ] Add UI/component test for setup validation if frontend test coverage exists
- [ ] Add UI/component test for successful setup submission/navigation if frontend test coverage exists
- [ ] Otherwise verify manually and cover full flow in 04d

## Verification

### Manual/UI smoke verification

Using an invited authenticated user:

- [ ] Navigating to `/interviews` loads without errors
- [ ] Existing interviews are displayed with role/topic/seniority/type/progress/status
- [ ] Empty state is shown when no interviews exist
- [ ] Loading state is visible while interviews are being fetched
- [ ] Error state is shown if `GET /api/interviews` fails
- [ ] Clicking “New interview” navigates to `/interviews/new`
- [ ] `/interviews/new` renders the setup form
- [ ] Submitting an empty form shows validation errors
- [ ] Submitting a valid form calls `POST /api/interviews`
- [ ] Successful creation navigates to `/interviews/{id}`
- [ ] API error during creation is displayed and the form remains editable
- [ ] Cancel/back link returns to `/interviews`
- [ ] Continue action for an active interview navigates to `/interviews/{id}`
- [ ] Basic view action for a completed interview navigates to `/interviews/{id}`

### Access verification

- [ ] Anonymous user navigating to `/interviews` is redirected to `/login`
- [ ] Anonymous user navigating to `/interviews/new` is redirected to `/login`
- [ ] Non-invited authenticated user sees access denied for `/interviews`
- [ ] Non-invited authenticated user sees access denied for `/interviews/new`

### Regression verification

- [ ] Existing frontend behavior still works
- [ ] Existing tests continue to pass
- [ ] Full M04 end-to-end flow is covered later in 04d

## Dependencies and Blockers

Depends on:

- 04a - Interview API
- Existing authentication UI behavior
- Existing invite/access handling

Blocks:

- 04c - Active interview UI
- 04d - End-to-end text flow verification and documentation

## Risks and Decisions

Risks:

- The list page depends on the shape of `GET /api/interviews`.
- The setup page depends on the response shape of `POST /api/interviews`.
- Completed interview viewing is intentionally minimal in M04 and may need expansion in M06.

Decisions:

- `/interviews` is a simple list/resume page, not a dashboard.
- Dashboard analytics remain in M07.
- Full completed interview history/review remains in M06.
- Role/topic/seniority/interview type options are hardcoded in M04.
- Status filtering UI is not required for M04. The page can show both active and completed interviews together.

## Notes

Routes:

```text
/interviews
/interviews/new
/interviews/{id}
```

API calls used by this feature:

```http
GET  /api/interviews
POST /api/interviews
```

The active interview page itself is implemented in 04c.

Completed interview links can navigate to `/interviews/{id}`, but in M04 this only needs to show a basic completed/read-only state. Full history, questions, answers, feedback, and summaries are handled later in M06.

Suggested setup request body:

```json
{
  "role": "backend-engineer",
  "seniority": "senior",
  "topic": "dotnet",
  "interviewType": "technical",
  "questionCount": 3
}
```

Expected successful creation response includes an `id` that can be used for navigation:

```text
/interviews/{id}
```

---
