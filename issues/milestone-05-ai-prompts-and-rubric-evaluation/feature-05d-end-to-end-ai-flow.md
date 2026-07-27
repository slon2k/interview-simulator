# 05d - End-to-end AI flow verification and documentation

Phase: 2  
Milestone: 05 - AI prompts and rubric evaluation  
Type: Feature  
Status: Planned

## Summary

Verify that the full interview flow works end-to-end with real AI generation and evaluation enabled, that it still works with stubs in CI, and update architecture documentation and ADRs to reflect the AI boundary introduced in M05.

## Problem and User Value

05a, 05b, and 05c each ship a slice of the AI integration. This feature confirms they compose correctly into a working experience — setup → adaptive questions → structured feedback → completion — and captures the resulting design decisions in the docs so the project stays interpretable.

## Scope

- Add an end-to-end integration test (behind a faked AI boundary) covering:
  - setup → answer 3 questions with feedback → complete
- Manually verify the same flow against real Azure OpenAI in a dev environment
- Confirm the stub path keeps CI credential-free
- Confirm prompt versions are recorded across a full session
- Confirm AI-failure degradation behaves correctly in the full flow
- Update `docs/architecture.md` with the AI boundary (generation + evaluation + validation)
- Add/adjust ADRs for AI response validation and prompt versioning (ADR 0009)
- Update `docs/milestones.md` M05 status

## Out of Scope

- Any new API endpoints or UI (M05 does not change the API/UI surface)
- Final summaries (M06), analytics (M07), voice (M08)

## Acceptance Criteria

- [ ] End-to-end integration test covers setup → 3 answered questions with feedback → complete, behind a faked AI boundary
- [ ] Real Azure OpenAI flow verified manually in dev (generation + evaluation)
- [ ] Stub-configured flow runs in CI without Azure OpenAI credentials
- [ ] Prompt versions are recorded across a full session
- [ ] AI-failure degradation verified in the full flow (session remains resumable)
- [ ] `docs/architecture.md` updated to describe the AI boundary
- [ ] ADR added for AI response validation and prompt versioning
- [ ] `docs/milestones.md` M05 acceptance criteria checked off
- [ ] All existing tests pass

## Tasks

### [ ] Verification

- [ ] Add end-to-end integration test (faked AI boundary)
- [ ] Manual dev verification against real Azure OpenAI
- [ ] Confirm CI credential-free stub path
- [ ] Confirm prompt-version recording across a session
- [ ] Confirm AI-failure degradation in full flow

### [ ] Documentation

- [ ] Update `docs/architecture.md`
- [ ] Add ADR 0009 (AI validation + prompt versioning)
- [ ] Update `docs/milestones.md` M05 status

## Verification

- [ ] A full interview completes with real AI in dev
- [ ] A full interview completes with stubs in CI
- [ ] Each answer returns rubric-based feedback
- [ ] Session turns record prompt versions
- [ ] Injected AI failure leaves the session resumable
- [ ] Documentation reflects the implemented AI boundary
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 05a - AI question generation
- 05b - Rubric-based answer evaluation
- 05c - Prompt versioning and AI response validation/error handling

Blocks:

- 06 - Session history and summaries

## Risks and Open Questions

### Risks

- Manual real-AI verification is not repeatable in CI; the automated test must rely on the faked boundary, so real-AI regressions can only be caught manually.

## Notes

This is the milestone's integration and documentation gate. It should not introduce new production behavior beyond wiring and verification.

M05 exit is reached when this feature's acceptance criteria are met and 05a–05c are merged.
