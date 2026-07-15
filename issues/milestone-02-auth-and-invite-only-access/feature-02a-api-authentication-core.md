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
- Add backend auth configuration and environment settings

## Out of Scope

- Invite-only authorization logic
- Frontend login/logout UX and route handling
- Additional identity providers such as Entra ID

## Traceability

- Phase: 1
- Milestone: 02 - Auth and invite-only access
- Related ADRs: ADR 0001, ADR 0003, ADR 0004, ADR 0008 in `docs/decisions.md`
- Related docs: `docs/roadmap.md`, `docs/milestones.md`, `docs/architecture.md`
- Requirement IDs: FR-002, FR-003, FR-004

Acceptance Criteria:

- [ ] GitHub OAuth login callback flow works in ASP.NET Core
- [ ] Authenticated session cookie is issued and validated correctly
- [ ] GitHub numeric user id is captured and normalized as `github|{githubUserId}`
- [ ] `/api/me` returns `isAuthenticated`, `isInvited`, `isAdmin`, `userId`, `identityProvider`, and `displayName`
- [ ] Backend auth configuration is documented for local and deployed environments

## Sub-Issues

- [ ] Task: Configure GitHub OAuth provider in ASP.NET Core
- [ ] Task: Implement auth callback and session cookie flow
- [ ] Task: Normalize GitHub numeric user ID in auth context
- [ ] Task: Implement `/api/me` endpoint with auth flags
- [ ] Task: Add auth smoke tests for authenticated and anonymous calls
- [ ] Task: Document auth configuration variables and callback URLs

## Verification

- [ ] Login flow succeeds and session cookie is present
- [ ] `/api/me` returns authenticated identity payload when logged in
- [ ] `/api/me` is denied or empty when not authenticated

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
