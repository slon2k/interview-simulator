# 02b - Invite-only authorization policy

Phase: 1
Milestone: 02 - Auth and invite-only access
Type: Feature
Status: Planned

## Summary

Implement invite-only authorization policy and enforce it on protected API endpoints.

## Problem and User Value

Authentication alone does not restrict access to invited users. Invite-only enforcement is required to keep the MVP private and controlled.

This feature ensures only invited authenticated users can use protected application functionality.

## Scope

- Define invite-only policy in ASP.NET Core authorization
- Implement config-based invite source strategy using admin and invited GitHub ID allowlists
- Enforce policy for protected API endpoints
- Return consistent forbidden or access-denied responses for non-invited users
- Define and document concrete allowlist configuration shape (example):
  - `AccessControl:AdminUserIds: []`
  - `AccessControl:InvitedUserIds: []`

## Out of Scope

- Admin UI for invite management
- Enterprise role hierarchy beyond invite-only
- Additional provider-specific authorization models

## Acceptance Criteria

- [x] Invite-only policy is implemented in backend authorization
- [x] Protected API endpoints require authenticated invited users
- [x] Non-invited authenticated users are denied protected endpoints
- [x] Admin GitHub IDs are treated as invited users
- [x] Config-based admin and invited allowlists are documented
- [x] `/api/me` includes `isInvited` and `isAdmin` based on current allowlist configuration.
- [x] Unauthorized and forbidden responses for protected `/api/*` endpoints are returned as JSON with `401`/`403` status codes (no HTML redirect payloads).

## Sub-Issues

- [x] Task: Define invite-only policy and policy handlers
- [x] Task: Implement config-based invite allowlist strategy
- [x] Task: Treat admin GitHub IDs as invited users
- [x] Task: Apply policy to protected endpoints
- [x] Task: Add authorization tests for invited and non-invited users
- [x] Task: Document policy behavior and expected denial responses

## Verification

- [x] Invited users can access protected endpoints
- [x] Admin users are treated as invited users
- [x] Non-invited authenticated users receive a clear forbidden response/page
- [x] Anonymous users are challenged or denied as expected
- [x] Protected `/api/*` endpoints return JSON payloads for `401` and `403` cases.

## Dependencies and Blockers

Blocked by:

- 02a - API authentication core

Blocks:

- 02c - Web authentication UX
- 03a - Cosmos DB persistence baseline
- Phase 2 interview flow features

## Risks and Open Questions

- Risk: Configuration changes require redeploy or restart to update invitations
- Open question: Should allowlist configuration remain app settings or move to a small data store later?
