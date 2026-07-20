# 02c - Web authentication UX

Phase: 1
Milestone: 02 - Auth and invite-only access
Type: Feature
Status: Planned

## Summary

Implement frontend authentication UX for login, logout, auth state bootstrap, and access-denied handling.

## Problem and User Value

Even with backend auth in place, users need a clear and consistent UI flow to sign in, sign out, and understand access restrictions.

This feature provides a usable authentication experience aligned with backend authorization behavior.

## Scope

- Add login and logout entry points in the web app
- Implement frontend auth state bootstrap from backend user context, including `isAuthenticated`, `isInvited`, and `isAdmin`
- Implement route-level UX guards for protected pages
- Implement access-denied UX for authenticated non-invited users
- Integrate frontend auth actions with backend auth endpoints:
  - Login via `GET /api/auth/login`
  - Logout via `POST /api/auth/logout`
  - Auth bootstrap via `GET /api/me`

## Out of Scope

- Backend policy enforcement logic
- Invite management UI
- Multi-provider login selector

## Acceptance Criteria

- [ ] Users can trigger login and logout from the frontend
- [ ] Frontend can resolve current auth state from backend user context with `isAuthenticated`, `isInvited`, and `isAdmin`
- [ ] Protected routes have UX-level guards aligned with backend policy
- [ ] Non-invited users see a clear access-denied experience
- [ ] Frontend login and logout are wired to `GET /api/auth/login` and `POST /api/auth/logout` respectively.
- [ ] Frontend handles backend `401` and `403` API responses predictably (unauthenticated vs authenticated-but-not-invited states).

## Sub-Issues

- [ ] Task: Add login and logout controls to frontend shell
- [ ] Task: Implement auth state bootstrap using backend endpoint(s)
- [ ] Task: Add protected route guard pattern for frontend pages
- [ ] Task: Implement access-denied page and routing behavior
- [ ] Task: Integrate login action to `GET /api/auth/login` and logout action to `POST /api/auth/logout`
- [ ] Task: Handle `401` and `403` API responses in route guard and global API error handling
- [ ] Task: Add frontend auth flow smoke tests

## Verification

- [ ] Authenticated invited user can navigate protected pages
- [ ] Authenticated non-invited user sees access-denied UX
- [ ] Logged-out user is redirected to login path for protected pages
- [ ] Login button triggers backend auth challenge endpoint and returns user to app after successful auth callback.

## Dependencies and Blockers

Blocked by:

- 02a - API authentication core
- 02b - Invite-only authorization policy

Blocks:

- Phase 2 interview flow features
- Session history and dashboard frontend features

## Risks and Open Questions

- Risk: Frontend route guards may drift from backend policy behavior
- Risk: Ambiguous unauthorized states can degrade user trust if UX is unclear
