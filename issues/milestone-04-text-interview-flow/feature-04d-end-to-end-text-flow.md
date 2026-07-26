# 04d - End-to-end text flow verification and documentation

Phase: 2
Milestone: 04 - Text interview flow
Type: Feature
Status: Planned

## Summary

Comprehensive integration test covering full happy path: list page → setup → active interview → answer questions → complete. Update architecture and development documentation to reflect M04 additions.

## Problem and User Value

Validates that all M04 pieces work together. Documents the new interview flow for future maintainers and as reference for phase 2 features.

## Scope

- Write integration test covering: navigate to `/interviews` → `/interviews/new` → fill form → submit → POST `/api/interviews` → navigate to `/interviews/{id}` → answer 3 questions → complete
- Test happy path with invited authenticated user
- Test auth scenarios: anonymous redirected to login, non-invited redirected to access denied
- Verify data persistence: interview and turns saved to Cosmos (or in-memory during test)
- Update `architecture.md` with:
  - Interview API endpoints and request/response examples
  - Interview page flow diagram
  - Question generation strategy (stub → M05 real AI)
- Update `development.md` with:
  - Interview setup and active session workflows
  - `IQuestionGenerator` extension points for M05
  - Local testing notes
- Update `docs/decisions.md` if any new ADRs needed (likely not)
- Verify all existing tests pass (46 unit + 13 integration)
- Clean up temporary feature flags or test data

## Out of Scope

- Performance testing
- Load testing
- Edge case exhaustive coverage (handled by feature tests)

## Acceptance Criteria

- [ ] Integration test exists and passes for full happy path (list → setup → active → 3 answers → complete)
- [ ] Integration test covers auth scenarios (401 anonymous, 403 non-invited, 200 invited)
- [ ] Test verifies data saved to persistence layer
- [ ] `architecture.md` updated with:
  - Interview API endpoint examples
  - Request/response DTOs
  - Page flow diagram
  - Question generation flow (stub placeholder → M05 real AI)
- [ ] `development.md` updated with:
  - Interview workflow walkthrough
  - `IQuestionGenerator` interface documentation
  - Instructions for adding real AI in M05
- [ ] All existing tests pass (46 unit + 13 integration + new integration test)
- [ ] Code compiles with 0 warnings, 0 errors
- [ ] Build and test pipeline succeeds

## Sub-Issues

- [ ] Task: Write comprehensive E2E integration test
- [ ] Task: Update architecture.md with API examples and flows
- [ ] Task: Update development.md with workflow and extension points
- [ ] Task: Run full test suite and verify all tests pass
- [ ] Task: Run build and verify no warnings/errors

## Verification

- [ ] Integration test covers full happy path
- [ ] Auth test scenarios pass
- [ ] Documentation is clear and accurate
- [ ] All 59 tests pass (46 + 13 + 1 new)
- [ ] Build succeeds with 0 warnings
