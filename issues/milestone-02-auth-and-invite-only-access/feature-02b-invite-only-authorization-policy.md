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

## Out of Scope

- Admin UI for invite management
- Enterprise role hierarchy beyond invite-only
- Additional provider-specific authorization models

## Traceability

- Phase: 1
- Milestone: 02 - Auth and invite-only access
- Related ADRs: ADR 0001, ADR 0003, ADR 0004, ADR 0008 in `docs/decisions.md`
- Related docs: `docs/roadmap.md`, `docs/milestones.md`, `docs/architecture.md`
- Requirement IDs: FR-005, FR-006

Acceptance Criteria:

- [ ] Invite-only policy is implemented in backend authorization
- [ ] Protected API endpoints require authenticated invited users
- [ ] Non-invited authenticated users are denied protected endpoints
- [ ] Admin GitHub IDs are treated as invited users
- [ ] Config-based admin and invited allowlists are documented

## Sub-Issues

- [ ] Task: Define invite-only policy and policy handlers
- [ ] Task: Implement config-based invite allowlist strategy
- [ ] Task: Treat admin GitHub IDs as invited users
- [ ] Task: Apply policy to protected endpoints
- [ ] Task: Add authorization tests for invited and non-invited users
- [ ] Task: Document policy behavior and expected denial responses

## Verification

- [ ] Invited users can access protected endpoints
- [ ] Admin users are treated as invited users
- [ ] Non-invited authenticated users receive forbidden response
- [ ] Anonymous users are challenged or denied as expected

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
