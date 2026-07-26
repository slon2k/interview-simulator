# 04a - Interview API

Phase: 2  
Milestone: 04 - Text interview flow  
Type: Feature  
Status: Planned

## Summary

Implement the backend API for the core text-based interview flow.

This feature covers creating an interview, listing interviews, retrieving interview state, submitting answers, generating stubbed next questions, and completing an interview.

Questions are hardcoded/stubbed in M04. Real AI question generation and answer evaluation are planned for M05.

## Problem and User Value

M03 delivered the Cosmos DB persistence foundation and interview session/turn document models. M04 needs an API layer that turns those persistence models into a usable text interview flow.

This feature provides the backend needed for:

- Interview setup page
- Active interview page
- Interview list/resume page
- Persisted text interview state

Users should be able to start an interview, answer questions, resume an active interview, and complete it.

## Scope

- Add interview API endpoints:
  - `POST /api/interviews`
  - `GET /api/interviews`
  - `GET /api/interviews/{id}`
  - `POST /api/interviews/{id}/answers`
  - `POST /api/interviews/{id}/complete`
- Add request/response DTOs for interview API
- Add hardcoded/stubbed question generation for M04
- Add interview state handling for:
  - active interviews
  - completed interviews
  - current question
  - answered count
  - turn progression
- Persist interview state using existing M03 documents:
  - `CosmosSessionDocument`
  - `CosmosTurnDocument`
- Support listing current user’s interviews with optional status filter:
  - `active`
  - `completed`
- Ensure all interview operations are scoped to the authenticated user
- Ensure all endpoints require invited-user authorization
- Add unit tests for question generation and interview state transitions
- Add integration tests for endpoint happy path and authorization scenarios
- Update Swagger/OpenAPI documentation if applicable

## Out of Scope

- Real AI question generation
- Answer evaluation
- Scoring
- Rubrics
- Prompt versioning
- AI response validation
- AI retries/error handling
- Final interview summaries
- Dashboard analytics
- Admin cross-user interview access
- Voice input/output

## Acceptance Criteria

- [ ] `POST /api/interviews` accepts interview setup data:
  - `role`
  - `seniority`
  - `topic`
  - `interviewType`
  - `questionCount`
- [ ] `POST /api/interviews` creates a new persisted interview session with `status=active`
- [ ] `POST /api/interviews` creates the first persisted turn with a stubbed question
- [ ] `POST /api/interviews` returns the created interview state and first/current question
- [ ] `GET /api/interviews` returns the current user’s interview summaries
- [ ] `GET /api/interviews` supports optional `?status=active|completed` filtering
- [ ] `GET /api/interviews/{id}` returns current interview state needed to resume the interview
- [ ] `GET /api/interviews/{id}` includes the current question/current turn for active interviews
- [ ] `GET /api/interviews/{id}` does not return full turn/answer history in M04
- [ ] `GET /api/interviews/{id}` does not generate new questions
- [ ] `POST /api/interviews/{id}/answers` accepts answer text and the current `turnNumber`
- [ ] Invalid interview state transitions return `409 Conflict`
- [ ] Answer submission saves the answer to the existing current turn
- [ ] Answer submission creates the next stubbed question/turn when questions remain
- [ ] Answer submission increments `answeredCount`
- [ ] Answering the final question automatically marks the interview as `completed`
- [ ] `POST /api/interviews/{id}/complete` marks an active interview as `completed`
- [ ] Completing an interview sets `completedAt`
- [ ] Completed interviews cannot receive new answers
- [ ] Completed interviews cannot be completed again
- [ ] Duplicate, stale, or wrong-turn answer submissions are rejected
- [ ] Interview persistence uses existing `CosmosSessionDocument` and `CosmosTurnDocument`
- [ ] Existing M03 document ID formats are preserved:
  - `session|{guid:D}`
  - `turn|{guid:D}|{turnNumber:D3}`
- [ ] Public API returns the raw interview/session id, not the Cosmos document id
- [ ] Users can only list/retrieve/update their own interviews
- [ ] Admin users do not receive cross-user interview access in M04
- [ ] Anonymous users receive `401`
- [ ] Authenticated non-invited users receive `403`
- [ ] Invited users can create/list/retrieve/answer/complete their own interviews
- [ ] Unit tests cover question generator behavior
- [ ] Unit tests cover interview state transitions
- [ ] Integration tests cover endpoint happy path
- [ ] Integration tests cover authorization scenarios
- [ ] Existing tests continue to pass

## Tasks

### [ ] Interview persistence/query support

- [ ] Define interview API request/response DTOs
- [ ] Add hardcoded question generator
- [ ] Add interview state constants: `active`, `completed`
- [ ] Add interview service/state handling
- [ ] Add persistence access for listing sessions and loading session turns
- [ ] Add unit tests for state transitions

### [ ] Interview API endpoint implementation

- [ ] Implement `POST /api/interviews`
- [ ] Implement `GET /api/interviews`
- [ ] Implement `GET /api/interviews/{id}`
- [ ] Implement `POST /api/interviews/{id}/answers`
- [ ] Implement `POST /api/interviews/{id}/complete`

### [ ] Interview API tests and documentation

- [ ] Add integration tests for interview API happy path
- [ ] Add integration tests for authorization scenarios
- [ ] Update Swagger/OpenAPI documentation if applicable

## Verification

- [ ] `POST /api/interviews` creates a persisted session document
- [ ] `POST /api/interviews` creates turn 1 with a stubbed question
- [ ] Created session document uses id format `session|{guid:D}`
- [ ] Created turn document uses id format `turn|{guid:D}|001`
- [ ] `GET /api/interviews` returns only the current user’s interviews
- [ ] `GET /api/interviews?status=active` returns only active interviews
- [ ] `GET /api/interviews?status=completed` returns only completed interviews
- [ ] `GET /api/interviews/{id}` returns the persisted current question for active interviews without generating new turns
- [ ] `GET /api/interviews/{id}` does not return full turn/answer history in M04
- [ ] `POST /api/interviews/{id}/answers` saves the submitted answer
- [ ] `POST /api/interviews/{id}/answers` creates the next turn when questions remain
- [ ] Final answer marks interview as completed
- [ ] Duplicate answer submission is rejected
- [ ] Wrong or stale `turnNumber` is rejected
- [ ] Answer submission after completion is rejected
- [ ] Completing an already completed interview is rejected
- [ ] Anonymous requests return `401`
- [ ] Non-invited authenticated requests return `403`
- [ ] Invited user can complete the full API flow
- [ ] User cannot access another user’s interview
- [ ] Full test suite passes

## Dependencies and Blockers

Depends on:

- 03 - Cosmos DB persistence ✅
- 03c - Session and turn document models ✅
- Invite-only authorization from identity/access foundation ✅

Blocks:

- 04b - Interview list/setup UI
- 04c - Active interview UI
- 05 - AI prompts and rubric evaluation
- 06 - Session history and summaries

## Risks and Open Questions

### Risks

- The API may need query support beyond the current point-read repository abstraction.
- State transition logic may become more complex once AI evaluation is added in M05.

## Notes

Public API uses interview terminology:

```http
POST /api/interviews
GET  /api/interviews
GET  /api/interviews/{id}
POST /api/interviews/{id}/answers
POST /api/interviews/{id}/complete
```

Persistence remains session/turn based:

```text
CosmosSessionDocument
CosmosTurnDocument
```

M04 statuses:

```text
active
completed
```

Question generation should be behind a small abstraction so M05 can replace the hardcoded generator with real AI generation.

Example shape:

```csharp
public interface IQuestionGenerator
{
    Task<GeneratedQuestion> GenerateQuestionAsync(
        GenerateQuestionRequest request,
        CancellationToken cancellationToken = default);
}
```

The M04 implementation should be deterministic and hardcoded. It may use role, seniority, topic, interview type, and turn number. It does not need AI context yet.

Suggested answer request shape:

```json
{
  "turnNumber": 1,
  "text": "My answer..."
}
```

Including `turnNumber` allows the backend to reject duplicate or stale submissions.
