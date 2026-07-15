# 03a - Cosmos DB persistence baseline

Phase: 1
Milestone: 03 - Cosmos DB persistence
Type: Feature
Status: Planned

## Summary

Implement Cosmos DB persistence baseline with deterministic IDs, partition strategy, and repository abstraction.

## Problem and User Value

Without persistence foundation, interview session state cannot be reliably stored or retrieved.

This feature enables durable storage needed for session flow, history, and analytics.

## Scope

- Define session and turn data contracts
- Implement deterministic ID generation
- Implement repository abstraction with Cosmos DB backend
- Validate basic read and write behavior from API

## Out of Scope

- Full dashboard aggregation logic
- Advanced indexing optimization and tuning
- Data migration between storage systems

## Traceability

- Phase: 1
- Milestone: 03 - Cosmos DB persistence
- Related ADRs: ADR 0002, ADR 0006 in `docs/decisions.md`
- Related docs: `docs/roadmap.md`, `docs/milestones.md`, `docs/architecture.md`
- Requirement IDs: FR-023, FR-040, FR-044

Acceptance Criteria:

- [ ] Cosmos DB free tier account and container exist
- [ ] Partition key strategy is documented
- [ ] Repository abstraction is implemented
- [ ] Test read and write works from ASP.NET Core API
- [ ] Session and turn document models are defined

## Sub-Issues

- [ ] Task: Define session and turn data contracts
- [ ] Task: Implement deterministic ID generation strategy
- [ ] Task: Implement repository interfaces and Cosmos implementation
- [ ] Task: Add a test read and write API check path
- [ ] Task: Document partition and query strategy in architecture docs

## Verification

- [ ] Session and turn documents are persisted using expected ID formats
- [ ] Reads are performed with authenticated user partition key
- [ ] API-level read and write smoke checks pass
- [ ] Data model documentation matches implementation

## Dependencies and Blockers

Blocked by:

- 01b - Infrastructure as code baseline
- 01c - CI/CD pipeline
- 02a - API authentication core
- 02b - Invite-only authorization policy

Blocks:

- Phase 2 interview flow features
- Session history and dashboard features

## Risks and Open Questions

- Risk: Early data model shortcuts can create later query inefficiencies
- Risk: Inconsistent ID format enforcement across services
