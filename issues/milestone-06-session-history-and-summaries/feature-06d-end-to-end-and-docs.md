# 06d - End-to-end verification and documentation

Phase: 2  
Milestone: 06 - Session history and summaries  
Type: Feature  
Status: Partial

## Summary

Verify the full review experience end-to-end — complete an interview, generate a summary, browse history, open detail — and update documentation to reflect the review/summary additions.

Note: the API-side verification is in place, but the end-to-end UI/docs follow-up still needs to be completed.

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

- [ ] End-to-end integration test covers complete → summary → history → detail, behind a faked AI boundary
- [ ] Real summary generation verified manually in dev
- [ ] History/detail reads confirmed to make no AI calls
- [ ] Summary-failure degradation verified (session still reviewable)
- [ ] Stub-configured flow runs in CI without Azure OpenAI credentials
- [ ] `docs/architecture.md` updated
- [ ] `docs/milestones.md` M06 acceptance criteria checked off
- [ ] All existing tests pass

## Tasks

### [ ] Verification

- [ ] Add end-to-end integration test (faked AI boundary)
- [ ] Manual dev verification of real summary generation
- [ ] Confirm read paths make no AI calls
- [ ] Confirm summary-failure degradation

### [ ] Documentation

- [ ] Update `docs/architecture.md`
- [ ] Update `docs/milestones.md` M06 status

## Verification

- [ ] A completed interview yields a persisted summary
- [ ] History lists the completed interview
- [ ] Detail shows full history + summary
- [ ] Reads make no AI calls
- [ ] Injected summary failure leaves the session reviewable
- [ ] Full test suite passes

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
