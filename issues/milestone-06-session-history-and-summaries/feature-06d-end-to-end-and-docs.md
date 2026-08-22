# 06d - End-to-end verification and documentation

Phase: 2  
Milestone: 06 - Session history and summaries  
Type: Feature  
Status: In Progress

## Summary

Verify the full review experience end-to-end — complete an interview, generate a summary, browse history, open detail — and update documentation to reflect the review/summary additions.

Automated API verification and documentation updates are in progress. Real Azure OpenAI summary quality remains a manual development-environment check.

## Problem and User Value

06a, 06b, and 06c each ship a slice. This feature confirms they compose into a working review flow and captures the resulting design in the docs.

## Scope

- End-to-end integration test (faked AI boundary): complete an interview → summary persisted → detail read returns full history + summary
- Manual dev verification with real Azure OpenAI summary generation
- Confirm no AI calls occur on history/detail reads
- Confirm summary-failure degradation keeps sessions reviewable
- Update `docs/architecture.md` for the review/summary read paths
- Update `docs/milestones.md` M06 status

## Out of Scope

- New endpoints or UI beyond wiring/verification
- Dashboard analytics (M07)

## Acceptance Criteria

- [x] End-to-end integration test covers complete → summary → history → detail, behind a faked AI boundary
- [ ] Real summary generation verified manually in dev
- [x] History/detail reads confirmed to make no AI calls
- [x] Summary-failure degradation verified (session still reviewable)
- [x] Stub-configured flow runs in CI without Azure OpenAI credentials
- [x] `docs/architecture.md` updated
- [x] `docs/milestones.md` M06 acceptance criteria checked off
- [x] All existing tests pass

## Tasks

### [ ] Verification

- [x] Add end-to-end integration test (faked AI boundary)
- [ ] Manual dev verification of real summary generation
- [x] Confirm read paths make no AI calls
- [x] Confirm summary-failure degradation

### [ ] Documentation

- [x] Update `docs/architecture.md`
- [x] Update `docs/milestones.md` M06 status

## Verification

- [x] A completed interview yields a persisted summary
- [x] History lists the completed interview
- [x] Detail shows full history + summary
- [x] Reads make no AI calls
- [x] Injected summary failure leaves the session reviewable
- [x] Full test suite passes

## Dependencies and Blockers

Depends on:

- 06a - Session detail read API
- 06b - Session summary generation
- 06c - History and session detail UI

Blocks:

- 07 - Dashboard analytics

## Risks and Open Questions

### Risks

- Real-AI summary quality can only be checked manually; automated tests rely on the faked boundary.

## Notes

Integration and documentation gate for M06. No new production behavior beyond wiring and verification.
