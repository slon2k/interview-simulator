# 02a - API authentication core

Phase: 1
Milestone: 02 - Auth and invite-only access
Type: Feature
Status: Planned

## Summary

Implement backend authentication core in ASP.NET Core using GitHub OAuth, secure cookie sessions, and normalized GitHub numeric identity.

## Problem and User Value

Without backend authentication plumbing, the application cannot establish trusted user identity for protected APIs.

This feature provides the identity foundation for invite-only authorization and authenticated user flows.

## Scope

- Configure GitHub OAuth provider and callback flow in ASP.NET Core
- Implement session cookie issuance and validation
- Normalize authenticated GitHub numeric user ID as `github|{githubUserId}`
- Implement `/api/me` endpoint for authenticated user identity and auth flags
- Implement explicit backend auth endpoints and callback contract:
  - `GET /api/auth/login` initiates challenge to GitHub
  - `GET /api/auth/callback` completes sign-in and issues session cookie
  - `POST /api/auth/logout` signs out and clears session cookie
- Add backend auth configuration and environment settings

## Out of Scope

- Invite-only authorization logic
- Frontend login/logout UX and route handling
- Additional identity providers such as Entra ID

## Acceptance Criteria

- [ ] GitHub OAuth login callback flow works in ASP.NET Core.
- [ ] Authenticated session cookie is issued and validated correctly.
- [ ] GitHub numeric user id is captured.
- [ ] Authenticated app user id is normalized as `github|{githubUserId}`.
- [ ] `/api/me` returns `isAuthenticated`, `userId`, `identityProvider`, and `displayName`.
- [ ] `/api/me` returns anonymous state when no user is logged in.
- [ ] `GET /api/auth/login`, `GET /api/auth/callback`, and `POST /api/auth/logout` are implemented and documented.
- [ ] API auth failures and authorization denials return JSON `401`/`403` responses for `/api/*` routes (no unexpected HTML redirect payloads).
- [ ] Backend auth configuration is documented for local and deployed environments.

## Sub-Issues

- [ ] Task: Configure GitHub OAuth provider in ASP.NET Core
- [ ] Task: Implement auth callback and session cookie flow
- [ ] Task: Implement `/api/auth/login`, `/api/auth/callback`, and `/api/auth/logout` endpoints
- [ ] Task: Normalize GitHub numeric user ID in auth context
- [ ] Task: Implement `/api/me` endpoint with auth flags
- [ ] Task: Add auth smoke tests for authenticated and anonymous calls
- [ ] Task: Document auth configuration variables and callback URLs

## Verification

- [ ] Login flow succeeds and session cookie is present
- [ ] `/api/me` returns authenticated identity payload when logged in
- [ ] `/api/me` returns a consistent anonymous payload when no user is logged in
- [ ] `/api/*` endpoints return JSON `401`/`403` on unauthorized or forbidden access without HTML auth redirect payloads.

## Dependencies and Blockers

Blocked by:

- 01a - Workspace and project skeleton
- 01c - CI/CD pipeline
- 01d - Foundation API and service validation

Blocks:

- 02b - Invite-only authorization policy
- 02c - Web authentication UX
- 03a - Cosmos DB persistence baseline

## Risks and Open Questions

- Risk: OAuth callback URL mismatch between local and deployed environments
- Risk: Cookie policy misconfiguration can break session persistence
