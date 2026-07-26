# 04b - Interview setup UI

Phase: 2
Milestone: 04 - Text interview flow
Type: Feature
Status: Planned

## Summary

Implement interview setup form at `/interviews/new`. Allows users to configure role, seniority, topic, and interview type, then POST to `/api/interviews` and navigate to active interview.

## Problem and User Value

04a provides the backend. This feature delivers the UI entry point. Users need a form to start a new interview and land on the active session page.

## Scope

- Create `/interviews/new` route in React router
- Build setup form component with:
  - Role dropdown (backend-engineer, frontend-engineer, fullstack, data-engineer, etc.)
  - Seniority dropdown (junior, mid, senior)
  - Topic dropdown (dotnet, javascript, react, sql, etc.)
  - Interview type dropdown (technical, behavioral, system-design)
  - Question count input or selector
  - Submit button
  - Cancel/back link
- Handle form validation (all fields required)
- POST to `/api/interviews` with form values
- Handle success: navigate to `/interviews/{id}`
- Handle error: display error message, allow retry
- Add loading state during API call
- Protect route: redirect anonymous users to `/login`
- Test form submission flow

## Out of Scope

- Advanced role/topic management (hardcoded dropdowns OK)
- Role/topic recommendations based on user history
- Save incomplete form state

## Acceptance Criteria

- [ ] `/interviews/new` route exists
- [ ] Form renders with all required fields
- [ ] Form validation prevents submission with empty fields
- [ ] Submit button POSTs to `/api/interviews` with form values
- [ ] On success: redirect to `/interviews/{interviewId}`
- [ ] On error: display error message and allow retry
- [ ] Loading state shown during API call (spinner or disabled submit)
- [ ] Cancel button/link returns to previous page or `/interviews`
- [ ] Anonymous users redirected to `/login`
- [ ] Non-invited users see access denied (from `/interviews/new` or after login)
- [ ] Form values properly encoded in POST body
- [ ] Happy path E2E test covers form fill → submit → navigation

## Sub-Issues

- [ ] Task: Create setup form component with fields
- [ ] Task: Add form validation and error display
- [ ] Task: Implement API call to POST /api/interviews
- [ ] Task: Add success navigation and error handling
- [ ] Task: Add loading state UI
- [ ] Task: Protect route with auth guard
- [ ] Task: E2E test for happy path

## Verification

- [ ] Form renders without errors
- [ ] Empty submission blocked with validation message
- [ ] Submit calls `/api/interviews` with correct payload
- [ ] Success redirects to active interview page
- [ ] Error displays message and form remains editable
- [ ] Loading state visible during API call
- [ ] Anonymous users cannot access page
