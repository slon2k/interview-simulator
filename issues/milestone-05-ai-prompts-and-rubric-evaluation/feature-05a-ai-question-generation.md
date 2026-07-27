# 05a - AI question generation

Phase: 2  
Milestone: 05 - AI prompts and rubric evaluation  
Type: Feature  
Status: Planned

## Summary

Replace the M04 hardcoded `IQuestionGenerator` stub with a real Azure OpenAI implementation that produces adaptive, role/topic/seniority/interview-type-aware questions.

The M04 stub is retained (not deleted) so tests and credential-free CI continue to run without live Azure OpenAI access. Implementation selection is configuration-driven.

## Problem and User Value

M04 delivered the full interview lifecycle with deterministic hardcoded questions. The questions are the same regardless of role, topic, or prior answers, which is not a usable interview experience.

This feature makes questions:

- Relevant to the selected role, seniority, topic, and interview type
- Adaptive — later questions take prior turns into account
- Varied across sessions instead of a fixed script

## Scope

- Add a real Azure OpenAI implementation of the existing `IQuestionGenerator` interface
- Keep the M04 hardcoded implementation available for tests and offline/CI runs
- Select the active implementation via configuration (real vs stub)
- Build the generation prompt from:
  - role
  - seniority
  - topic
  - interview type
  - turn number
  - prior turns (question/answer history) for adaptivity
- Reuse the existing Azure OpenAI client wiring from `Startup/OpenAI.cs`
- Record the prompt version used on the generated turn (coordinated with 05c)
- Add unit tests using the stub and a faked evaluator/client boundary
- Add integration tests that exercise generation behind a fake AI boundary (no live calls in CI)

## Out of Scope

- Answer evaluation and scoring (05b)
- Prompt versioning infrastructure and response validation/error handling (05c) — this feature consumes those seams but 05c owns them
- Final summaries (M06)
- Dashboard analytics (M07)
- Voice (M08)

## Acceptance Criteria

- [ ] A real Azure OpenAI implementation of `IQuestionGenerator` exists
- [ ] The M04 hardcoded implementation remains available and is used by default in CI/tests
- [ ] Active implementation is selected by configuration, not by build
- [ ] Generated questions reflect role, seniority, topic, and interview type
- [ ] The next question uses prior turns as context (adaptive)
- [ ] The prompt version is recorded on the persisted turn
- [ ] Generation failures surface as a clear error and leave the session resumable (no partial/corrupt turn)
- [ ] The public API contract from M04 is unchanged
- [ ] CI does not require live Azure OpenAI credentials
- [ ] Unit tests cover prompt-building logic
- [ ] Integration tests cover generation behind a faked AI boundary
- [ ] Existing tests continue to pass

## Tasks

### [ ] Real generator implementation

- [ ] Add Azure OpenAI-backed `IQuestionGenerator` implementation
- [ ] Build prompt from session setup + prior turns
- [ ] Wire configuration-based selection of real vs stub in `Startup`
- [ ] Record prompt version on the generated turn

### [ ] Tests

- [ ] Unit tests for prompt construction
- [ ] Integration tests using a fake AI boundary
- [ ] Confirm stub path keeps CI credential-free

## Verification

- [ ] Starting an interview produces a context-appropriate first question
- [ ] Subsequent questions differ based on prior answers
- [ ] Turn documents record the prompt version
- [ ] With the stub configured, the full flow runs without Azure OpenAI credentials
- [ ] A simulated generation failure does not corrupt session state
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 04a - Interview API (defines `IQuestionGenerator` and turn persistence) ✅
- Azure OpenAI access validated in M01 ✅

Coordinates with:

- 05c - Prompt versioning and AI response validation/error handling

Blocks:

- 05d - End-to-end AI flow verification

## Risks and Open Questions

### Risks

- Adaptive prompts that include full history can grow token cost quickly — may need a bounded context window (last N turns) rather than all turns.
- Non-deterministic output makes assertions harder; tests should assert structure/shape, not exact wording.

### Open Questions

- How many prior turns to include as context before summarizing/truncating?

## Notes

Selection example:

```text
AzureOpenAI:Enabled = true   -> real generator
AzureOpenAI:Enabled = false  -> M04 hardcoded generator
```

The interface does not change from M04:

```csharp
public interface IQuestionGenerator
{
    Task<GeneratedQuestion> GenerateQuestionAsync(
        GenerateQuestionRequest request,
        CancellationToken cancellationToken = default);
}
```
