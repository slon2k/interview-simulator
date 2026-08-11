# 05d - End-to-end AI flow verification and documentation

Phase: 2  
Milestone: 05 - AI prompts and rubric evaluation  
Type: Feature  
Status: Planned

## Summary

Verify that the full interview flow works end-to-end with faked AI boundaries in CI, confirm the stub path is credential-free, add degradation tests for all AI failure points with explicit no-partial-save guarantees, and update architecture documentation and ADRs to reflect the AI boundary introduced in M05.

## Problem and User Value

05a, 05b, and 05c each ship a slice of the AI integration. This feature confirms they compose correctly into a working experience — setup → adaptive questions → per-answer structured feedback → completion — and captures the resulting design decisions in the docs so the project stays interpretable.

## Scope

- Add an end-to-end integration test (stub AI boundary) covering: setup → answer 3 questions with evaluation persisted on turns → complete
- Add degradation tests for all three AI failure points in the flow
- Confirm stub path keeps CI credential-free
- Confirm question generation prompt versions are recorded on all turns
- Confirm evaluation prompt versions are recorded on all answered turns
- Update `docs/architecture.md` with the AI boundary (generation + evaluation + validation + degradation)
- Add ADR 0010 for the AI boundary decisions: synchronous AI, prompt versioning in code, `AiStructuredOutputRunner`, 503 for AI failures, and separate generation/evaluation metadata
- Update `docs/milestones.md` M05 acceptance criteria

## Out of Scope

- Any new API endpoints or UI surface changes — M05 does not change the endpoint set
- Evaluation of final session summaries (M06)
- Cross-session analytics (M07)
- Voice input/output (M08)
- Application Insights wiring (M09)

## Acceptance Criteria

- [ ] End-to-end integration test: create → start → answer × 3 → complete
- [ ] Every answered turn in the E2E test has `EvaluationAiMetadata.PromptVersion` set
- [ ] Every turn in the E2E test has `QuestionGenerationMetadata.PromptVersion` set
- [ ] Stub-configured flow runs in CI without Azure OpenAI credentials
- [ ] Degradation test: start generation failure leaves session in `Created` state; session can be started again
- [ ] Degradation test: answer evaluation failure leaves current turn unanswered and session `Active`; the same answer can be resubmitted successfully
- [ ] Degradation test: evaluation success + next-question generation failure leaves current turn unanswered and session `Active`; resubmit succeeds
- [ ] On all AI failure paths, no partial turn/session persistence occurs (`SaveAnswerAsync` is not called for submit failures)
- [ ] `docs/architecture.md` updated with AI boundary section
- [ ] ADR 0010 added for AI boundary decisions
- [ ] ADR route naming is consistent with current interview endpoints
- [ ] `docs/milestones.md` M05 acceptance criteria checked off
- [ ] All existing tests pass

## Tasks

### E2E integration test

- [ ] Add `api/tests/InterviewSimulator.Api.IntegrationTests/Interviews/InterviewAiFlowEndToEndTests.cs`
  - Uses stub AI boundary (`StubAnswerEvaluator` + `SequencedQuestionGenerator` from `AuthWebApplicationFactory`)
  - Creates a session, starts it, submits 3 answers, completes it
  - Asserts each submit succeeds and flow reaches completion deterministically with stubs
  - Asserts persisted answered turns include evaluation with expected dimension count for interview type
  - Asserts persisted evaluations have overall score in 0-100 range
  - Asserts final session status is `Completed` and `answeredCount == 3`

### Prompt version recording test

- [ ] In the E2E test (or a separate test), assert that persisted turn documents contain:
  - `questionAi.promptVersion` on all turns
  - `evaluationAi.promptVersion` on all answered turns

### Degradation tests

- [ ] Add degradation test cases (can be in `SubmitAnswerEvaluationTests.cs` or a dedicated file)
  - Start generation failure: `IQuestionGenerator` throws a typed `AiException` (for example `AiProviderUnavailableException`); response is 503; `GET /api/interviews/{id}` returns `Status = Created`; retrying `POST /start` succeeds
  - Answer evaluation failure: `IAnswerEvaluator` throws a typed `AiException` (for example `AiProviderUnavailableException`); response is 503; `GET /api/interviews/{id}` returns `Status = Active` with `answeredCount` unchanged; resubmitting the same answer succeeds
  - Evaluation success + generation failure: evaluator succeeds but generator throws a typed `AiException`; response is 503; session/turn state unchanged; resubmit succeeds
  - Submit failure atomicity: assert `SaveAnswerAsync` is not called and the current turn remains unanswered/unevaluated

### Documentation

- [ ] Update `docs/architecture.md`
  - Add AI boundary section covering: synchronous AI-before-persistence model, `AiStructuredOutputRunner`, `IQuestionGenerator`, `IAnswerEvaluator`, prompt versioning, retry policy, error routing, and graceful degradation guarantees
- [ ] Add ADR 0010 to `docs/decisions.md`
  - Title: AI boundary, prompt versioning, and structured evaluation
  - Status: Accepted
  - Context: M05 introduces real AI calls; needs consistent versioning, validation, and failure handling
  - Decision: synchronous AI calls complete before Cosmos writes; prompts versioned as code constants; `AiStructuredOutputRunner` handles retry and parsing; typed AI exceptions map to 503; generation and evaluation metadata stored separately on turn documents; `OverallScore` computed by app from dimension average
  - Consequences: AI failures are always resumable; historical sessions are interpretable across prompt changes; Azure-specific details do not leak into endpoint code
  - Reconcile ADR route naming to interview endpoints (`/api/interviews/*`) so documentation matches implemented API
- [ ] Update `docs/milestones.md` — check off M05 acceptance criteria
  - Ensure M05 criteria explicitly mention resumable AI failures and no-partial-save behavior

### Manual real-AI verification (non-CI)

- [ ] Verify full flow against real Azure OpenAI with `Ai:Provider = AzureOpenAI`
  - Generated questions reflect role, seniority, interview type, and focus area
  - Answer evaluation returns per-dimension scores and feedback
  - Prompt versions are recorded on persisted turn documents

## Verification

- [ ] E2E test passes with faked AI boundary; no live Azure OpenAI credentials required
- [ ] All three degradation scenarios produce 503 and leave session state unchanged
- [ ] Retry after each degradation scenario succeeds
- [ ] Submit failure scenarios verify no partial persistence of answer/evaluation
- [ ] `docs/architecture.md` describes the full AI boundary
- [ ] ADR 0010 is present in `docs/decisions.md` decision index
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 05a - AI boundary foundation
- 05b - Azure OpenAI question generation
- 05c - Rubric-based answer evaluation

Blocks:

- 06 - Session history and summaries

## Risks and Open Questions

### Risks

- Manual real-AI verification is not repeatable in CI; regressions against live Azure OpenAI can only be caught manually or with a dedicated dev-environment test run.
