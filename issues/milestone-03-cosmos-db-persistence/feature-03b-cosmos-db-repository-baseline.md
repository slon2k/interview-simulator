# 03b - Cosmos DB repository baseline

Phase: 1
Milestone: 03 - Cosmos DB persistence
Type: Feature
Status: Planned

## Summary

Implement application persistence baseline with Cosmos DB SDK integration, session and turn data contracts, deterministic document IDs, and repository abstraction.

## Problem and User Value

After Cosmos DB infrastructure exists, the application needs a safe and consistent way to persist and retrieve interview session data.

This feature provides the persistence abstraction required by future interview flow and session history features.

## Scope

- Add Cosmos DB SDK integration to ASP.NET Core API
- Add Cosmos DB options and validation
- Define session and turn document contracts
- Implement deterministic document ID generation
- Implement domain-specific repository abstraction
- Implement Cosmos DB repository backend
- Add tests for ID generation and repository behavior
- Add protected dev-only persistence smoke path
- Document data model, ID format, partition strategy, and query strategy

## Out of Scope

- Cosmos DB infrastructure provisioning
- Full interview session workflow
- Dashboard aggregation logic
- Advanced indexing optimization
- Data migrations
- Public or unauthenticated persistence endpoints
- Cosmos DB emulator or Testcontainers setup

## Acceptance Criteria

- [ ] Cosmos DB SDK is integrated into the ASP.NET Core API
- [ ] CosmosDbOptions exists and binds from CosmosDb configuration section
- [ ] Cosmos DB options support managed identity configuration
- [ ] Cosmos DB options validation is implemented
- [ ] Session document model is defined
- [ ] Turn document model is defined
- [ ] Documents include id, type, schemaVersion, userId, and UTC timestamps
- [ ] Deterministic document ID generation strategy is implemented
- [ ] Deterministic document ID generation is covered by tests
- [ ] Repository abstraction is implemented
- [ ] Cosmos DB repository implementation is added
- [ ] Repository methods require authenticated normalized userId
- [ ] Reads are performed with authenticated user partition key
- [ ] API-level read and write smoke check works in dev
- [ ] Persistence smoke path is protected by authenticated invited-user authorization
- [ ] Persistence smoke path is disabled or unavailable in production
- [ ] Normal CI does not require a live Cosmos DB account
- [ ] Data model and query strategy documentation matches implementation

## Sub-Issues

- [ ] Task: Add Cosmos DB SDK and options binding
- [ ] Task: Define session and turn document contracts
- [ ] Task: Implement deterministic document ID generation strategy
- [ ] Task: Implement repository interface
- [ ] Task: Implement Cosmos DB repository
- [ ] Task: Add unit tests for ID generation and model validation
- [ ] Task: Add repository tests without live Cosmos dependency where practical
- [ ] Task: Add protected dev-only persistence smoke path
- [ ] Task: Document data model, ID format, partition, and query strategy

## Verification

- [ ] App starts successfully with Cosmos DB configuration
- [ ] App fails with clear configuration errors when Cosmos DB is enabled but required settings are missing
- [ ] Session documents use ID format session:{sessionId}
- [ ] Turn documents use ID format turn:{sessionId}:{turnIndex}
- [ ] Turn index is zero-padded for stable ordering
- [ ] Documents are partitioned by authenticated normalized userId, for example github|12345678
- [ ] Repository reads use partition key from authenticated user context
- [ ] Anonymous users cannot execute persistence smoke checks
- [ ] Authenticated non-invited users cannot execute persistence smoke checks
- [ ] Invited users can execute API-level read and write smoke check
- [ ] Admin users can execute API-level read and write smoke check
- [ ] API-level read and write smoke checks pass in deployed dev environment
- [ ] Normal CI passes without live Cosmos DB credentials
- [ ] Data model documentation matches implementation

## Dependencies and Blockers

Blocked by:

- 02a - API authentication core
- 02b - Invite-only authorization policy
- 03a - Cosmos DB IaC and configuration baseline

Blocks:

- Phase 2 interview flow features
- Session history and dashboard features
- Future analytics features

## Risks and Open Questions

Risks:

- Risk: Early data model shortcuts can create later query inefficiencies
- Risk: Inconsistent ID format enforcement across services can cause read and write bugs
- Risk: Using the wrong partition key can cause cross-partition queries or authorization bugs
- Risk: API smoke endpoints can become accidental public write endpoints if not protected
- Risk: Model shape may change as interview flow requirements become clearer

Open questions:

- Should the baseline use a single container with type discriminator for sessions and turns?
- Should session ID use GUID v7, ULID, or another sortable ID format?
- Should the persistence smoke path be removed after real session APIs exist?
- Should Cosmos DB indexing policy be customized after query patterns stabilize?
