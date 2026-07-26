# 03c - Session and turn document models

Phase: 1
Milestone: 03 - Cosmos DB persistence
Type: Feature
Status: Planned

## Summary

Define and implement session and turn document models, deterministic ID generation strategy, and smoke test endpoints for Cosmos DB persistence. Completes the session data persistence layer foundation needed for phase 2 interview flow features.

## Problem and User Value

Milestone 03 delivered Cosmos DB infrastructure (03a) and repository abstraction (03b). User persistence was implemented out of scope as a bridge for OAuth login. Now session and turn document models are needed to support interview session creation, answer submission, and history retrieval in phase 2.

This feature provides the concrete data contracts for storing interview sessions and turn-by-turn Q&A records.

## Scope

- Define `CosmosSessionDocument` model with session metadata and denormalized summary metrics
- Define `CosmosTurnDocument` model with question, answer, evaluation, and AI metadata
- Implement deterministic ID generation utility with format validation
- Add unit tests for ID generation and model validation
- Add integration test for session/turn CRUD operations (no live Cosmos required)
- Add protected smoke test endpoints for session/turn write verification
- Update architecture documentation to reflect final implementation
- Update data model diagrams if needed

## Out of Scope

- Full interview session workflow (phase 2)
- Dashboard aggregation queries
- Advanced indexing optimization
- Cosmos DB emulator or Testcontainers

## Acceptance Criteria

- [ ] `CosmosSessionDocument` is defined with id, userId, sessionId, type, schemaVersion, role, seniority, topic, interviewType, status, questionCount, answeredCount, timestamps, summary
- [ ] `CosmosTurnDocument` is defined with id, userId, sessionId, turnNumber, type, schemaVersion, question, answer, evaluation, aiMetadata, timestamps
- [ ] Session documents use deterministic ID format: `session|{sessionId}`
- [ ] Turn documents use deterministic ID format: `turn|{sessionId}|{turnNumber:00}` with zero-padded turn numbers
- [ ] Deterministic ID generation is unit-tested (format, padding, edge cases)
- [ ] Model validation tests exist for required fields
- [ ] Models implement `ICosmosDocument` interface
- [ ] Protected `/api/persistence/sessions` POST endpoint creates a session document
- [ ] Protected `/api/persistence/sessions/{sessionId}/turns` POST endpoint creates a turn document
- [ ] Protected `/api/persistence/sessions/{sessionId}` GET endpoint retrieves a session document
- [ ] Smoke test endpoints are invite-only authorized
- [ ] Anonymous users cannot access persistence smoke tests (401)
- [ ] Non-invited authenticated users cannot access persistence smoke tests (403)
- [ ] Invited and admin users can execute smoke tests successfully (200)
- [ ] All existing tests continue to pass
- [ ] Architecture documentation includes final session and turn schema
- [ ] Deterministic ID strategy is documented with examples

## Sub-Issues

- [ ] Task: Create `CosmosSessionDocument` model with full schema
- [ ] Task: Create `CosmosTurnDocument` model with full schema
- [ ] Task: Implement deterministic ID generation utility (`SessionIdGenerator` or similar)
- [ ] Task: Add unit tests for ID generation (format, padding, edge cases)
- [ ] Task: Add model validation tests
- [ ] Task: Create protected `/api/persistence/sessions` POST endpoint
- [ ] Task: Create protected `/api/persistence/sessions/{sessionId}` GET endpoint
- [ ] Task: Create protected `/api/persistence/sessions/{sessionId}/turns` POST endpoint
- [ ] Task: Add integration tests for smoke endpoints with auth scenarios
- [ ] Task: Update architecture.md with final implementation details
- [ ] Task: Document ID generation strategy and examples

## Verification

- [ ] `CosmosSessionDocument` compiles and implements `ICosmosDocument`
- [ ] `CosmosTurnDocument` compiles and implements `ICosmosDocument`
- [ ] Session ID generation produces `session|{id}` format
- [ ] Turn ID generation produces `turn|{sessionId}|{number}` with zero-padded numbers
- [ ] ID generation tests cover edge cases (empty strings, special characters, max length)
- [ ] Session POST endpoint stores document in Cosmos (or no-op when disabled)
- [ ] Session GET endpoint retrieves document by sessionId with authenticated userId partition key
- [ ] Turn POST endpoint stores document in Cosmos (or no-op when disabled)
- [ ] Authorization tests verify anonymous/non-invited users cannot access
- [ ] Authorization tests verify invited/admin users can access
- [ ] All 15 existing tests continue to pass after changes
- [ ] Architecture documentation matches implementation

## Dependencies and Blockers

Depends on:

- 03a - Cosmos DB IaC and configuration baseline ✅
- 03b - Cosmos DB repository baseline ✅

Blocks:

- 04 - Text interview flow (session creation, answer submission)
- Phase 2 session history and dashboard features

## Risks and Open Questions

Risks:

- Risk: Session and turn document schema may evolve during phase 2 (evaluation rubric, AI model versioning)
- Risk: Deterministic ID strategy may need adjustments if query patterns change

Questions:

- Should session documents store full turn history or only summary metadata? (Decision: summary only, turns stored separately)
- Should turn documents be separately paginated or fetched with session? (Decision: separate containers/queries for flexibility)

## Notes

This issue captures out-of-scope work identified during 03b implementation:

- User persistence was implemented as a bridge for OAuth login to work end-to-end
- Session/turn models were documented in architecture.md but not implemented
- This issue brings those models into code and adds tests

Prioritizing this before phase 2 ensures the data layer is fully specified and tested.
