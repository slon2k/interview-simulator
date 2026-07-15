# 01d - Foundation API and service validation

Phase: 1
Milestone: 01 - Foundation and Azure skeleton
Type: Feature
Status: Planned

## Summary

Establish baseline API readiness and validate Azure OpenAI, Azure Speech token flow, and foundation setup documentation.

## Problem and User Value

Without early service validation, phase 2 features can fail late due to environment or integration misconfiguration.

This feature reduces integration risk by confirming core dependencies before full interview flow implementation.

## Scope

- Implement baseline health endpoint
- Validate OpenAI connectivity and model access
- Validate speech token issuance path
- Finalize foundation setup and troubleshooting notes

## Out of Scope

- Full interview session business logic
- Rubric evaluation and scoring behavior
- Dashboard and history features

## Traceability

- Phase: 1
- Milestone: 01 - Foundation and Azure skeleton
- Related ADRs: ADR 0001, ADR 0007 in `docs/decisions.md`
- Related docs: `docs/roadmap.md`, `docs/milestones.md`, `docs/architecture.md`
- Requirement IDs: FR-060 and foundation platform checks

Acceptance Criteria:

- [ ] API endpoint `/api/health` works
- [ ] Azure OpenAI access and model deployment are validated
- [ ] Azure Speech token flow is validated
- [ ] Basic local development flow is documented

## Sub-Issues

- [ ] Task: Implement `/api/health` endpoint
- [ ] Task: Implement OpenAI connectivity validation endpoint or startup check
- [ ] Task: Implement `/api/speech/token` endpoint and validate token flow
- [ ] Task: Add foundation troubleshooting notes for common setup failures
- [ ] Task: Finalize phase 1 foundation setup documentation

## Verification

- [ ] Health endpoint returns expected successful response
- [ ] OpenAI validation call succeeds in development environment
- [ ] Speech token endpoint returns valid temporary token
- [ ] Setup docs allow repeatable local validation by another developer

## Dependencies and Blockers

Blocked by:

- 01a - Workspace and project skeleton
- 01b - Infrastructure as code baseline
- 01c - CI/CD pipeline

Blocks:

- 02a - API authentication core
- 02b - Invite-only authorization policy
- 03a - Cosmos DB persistence baseline

## Risks and Open Questions

- Risk: Service quota, access, or model deployment mismatch in Azure OpenAI
- Risk: Speech configuration differences between local and deployed environments
