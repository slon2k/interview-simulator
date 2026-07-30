# Milestone 04 - Text interview flow (placeholder)

Epic type: Milestone

## Overview

Build the core text-based interview experience with stub question generation. All endpoints and UI work end-to-end, but questions are hardcoded placeholders. M05 replaces question generation stubs with real Azure OpenAI integration.

This milestone proves the full session lifecycle works without introducing AI complexity.

## Feature Issues

- 04a - Interview API (CRUD + list with status filter)
- 04b - Interview setup UI
- 04c - Active interview UI
- 04d - End-to-end text flow verification and documentation

## Key Decisions

- **Naming**: "Interview" instead of "Session" aligns with domain language
- **Question Generation**: Stubbed via `IQuestionGenerator` (hardcoded impl) — M05 swaps in real OpenAI
- **List Page**: `/interviews` enables users to view and resume unfinished interviews
- **Routes**:
  - `/interviews` — list/resume page
  - `/interviews/new` — setup form
  - `/interviews/{id}` — active interview
- **API**:
  - `POST /api/interviews` — create (starts in `created` status)
  - `POST /api/interviews/{id}/start` — start interview and generate first question
  - `GET /api/interviews` — list (supports `created`, `active`, `completed` and multi-status filtering)
  - `GET /api/interviews/{id}` — retrieve
  - `POST /api/interviews/{id}/answers` — submit answer
  - `POST /api/interviews/{id}/complete` — finish

## Exit Criteria

- All 4 features shipped and merged
- Happy path integration test covers setup → 3 questions → complete
- UI pages render without errors
- All existing tests pass (46 unit + 13 integration)
- Architecture documentation updated

## Notes

Placeholder question generator returns a simple hardcoded question per topic. No OpenAI calls, no prompt versioning, no evaluation. Fully testable without external services.

**Query Support (Decision)**: M04 query support is implemented in the interview store layer (concrete Cosmos store), not via a generic repository abstraction.

For `GET /api/interviews`, status filtering supports:

1. Single value: `?status=created`
2. Multi-value: `?status=created&status=active`

Invalid status values return `400` validation errors.
