# 07c - End-to-end verification, documentation, and Phase 2 exit

Phase: 2  
Milestone: 07 - Dashboard analytics  
Type: Feature  
Status: Planned

## Summary

Verify the dashboard end-to-end, update documentation, and confirm the Phase 2 (Text Interview MVP) exit criteria are met. This is the closing feature of Phase 2.

## Problem and User Value

07a and 07b ship the analytics API and UI. This feature confirms they compose correctly and, more broadly, validates that the whole Phase 2 experience works: run interviews, get feedback, review history and summaries, and see progress — the complete text MVP.

## Scope

- End-to-end integration test: seed completed sessions → dashboard endpoint returns correct aggregates
- Manual verification of the dashboard UI against seeded/real data
- Confirm analytics make no AI calls
- Confirm analytics are user-scoped
- Update `docs/architecture.md` with the analytics read model
- Update `docs/milestones.md` M07 status
- Verify and record Phase 2 exit criteria from `docs/roadmap.md`

## Out of Scope

- New endpoints or UI beyond wiring/verification
- Phase 3 items (voice, observability, demo readiness)

## Acceptance Criteria

- [ ] End-to-end integration test covers seeded sessions → correct dashboard aggregates
- [ ] Dashboard UI verified against seeded/real data
- [ ] Analytics confirmed to make no AI calls
- [ ] Analytics confirmed user-scoped
- [ ] `docs/architecture.md` updated with the analytics read model
- [ ] `docs/milestones.md` M07 acceptance criteria checked off
- [ ] Phase 2 exit criteria (roadmap) verified and recorded
- [ ] All existing tests pass

## Tasks

### [ ] Verification

- [ ] Add end-to-end integration test for aggregation correctness
- [ ] Manual dashboard UI verification
- [ ] Confirm no AI calls in analytics
- [ ] Confirm user scoping

### [ ] Documentation and Phase 2 exit

- [ ] Update `docs/architecture.md`
- [ ] Update `docs/milestones.md` M07 status
- [ ] Walk through and record Phase 2 exit criteria from `docs/roadmap.md`

## Verification

- [ ] Dashboard aggregates match seeded data
- [ ] UI renders all basic metrics correctly
- [ ] No AI calls occur for analytics
- [ ] A user sees only their own analytics
- [ ] Phase 2 exit criteria are satisfied:
  - user can complete a full text interview
  - each answer receives structured rubric feedback
  - sessions are saved in Cosmos DB
  - completed sessions have summaries
  - user can review past sessions
  - user can see basic progress information
  - app is usable by invited users without voice
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 07a - Dashboard analytics API
- 07b - Dashboard UI
- All of M04, M05, M06 (the full Phase 2 experience)

Blocks:

- Phase 3 entry (08 - Voice UX with Azure Speech)

## Risks and Open Questions

### Risks

- Phase 2 exit depends on all prior milestones being genuinely complete; this feature may surface gaps from earlier milestones that need small follow-ups.

## Notes

This is both the M07 gate and the Phase 2 gate. It should not introduce new production behavior beyond wiring, verification, and documentation. Phase 2 exit criteria are defined in `docs/roadmap.md` under "Phase 2 - Text Interview MVP".
