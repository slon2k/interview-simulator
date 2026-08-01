# 04a - Interview API

Phase: 2  
Milestone: 04 - Text interview flow  
Type: Feature  
Status: Completed

## Summary

Implement the backend API for the core text-based interview flow.

This feature covers creating an interview, explicitly starting it, listing interviews, retrieving interview state, submitting answers, generating stubbed next questions, and completing an interview.

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
  - `POST /api/interviews/{id}/start`
  - `GET /api/interviews`
  - `GET /api/interviews/{id}`
  - `POST /api/interviews/{id}/answers`
  - `POST /api/interviews/{id}/complete`
- Add request/response DTOs for interview API
- Add hardcoded/stubbed question generation for M04
- Add interview state handling for:
  - created interviews
  - active interviews
  - completed interviews
  - current question
  - answered count
  - turn progression
- Persist interview state using existing M03 documents:
  - `CosmosSessionDocument`
  - `CosmosTurnDocument`
- Support listing current user’s interviews with optional status filter:
  - `created`
  - `active`
  - `completed`
  - multi-status query (for example repeated `status` query parameters)
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

- [x] `POST /api/interviews` accepts interview setup data:
  - `role`
  - `seniority`
  - `topic`
  - `interviewType`
  - `questionCount`
- [x] `POST /api/interviews` creates a new persisted interview session with `status=created`
- [x] `POST /api/interviews` does not create the first turn yet
- [x] `POST /api/interviews` returns the created interview state
- [x] `POST /api/interviews/{id}/start` transitions interview state from `created` to `active`
- [x] `POST /api/interviews/{id}/start` creates the first persisted turn with a stubbed question
- [x] `POST /api/interviews/{id}/start` returns active interview state and first/current question
- [x] `GET /api/interviews` returns the current user’s interview summaries
- [x] `GET /api/interviews` supports status filtering for `created`, `active`, and `completed`
- [x] `GET /api/interviews` supports multi-status filtering
- [x] `GET /api/interviews/{id}` returns current interview state needed to resume the interview
- [x] `GET /api/interviews/{id}` includes the current question/current turn for active interviews
- [x] `GET /api/interviews/{id}` does not return full turn/answer history in M04
- [x] `GET /api/interviews/{id}` does not generate new questions
- [x] `POST /api/interviews/{id}/answers` accepts answer text and the current `turnNumber`
- [x] Invalid interview state transitions return `409 Conflict`
- [x] Answer submission saves the answer to the existing current turn
- [x] Answer submission creates the next stubbed question/turn when questions remain
- [x] Answer submission increments `answeredCount`
- [x] Answering the final question automatically marks the interview as `completed`
- [x] `POST /api/interviews/{id}/complete` marks an active interview as `completed`
- [x] Completing an interview sets `completedAt`
- [x] Completed interviews cannot receive new answers
- [x] Completed interviews cannot be completed again
- [x] Duplicate, stale, or wrong-turn answer submissions are rejected
- [x] Interview persistence uses existing `CosmosSessionDocument` and `CosmosTurnDocument`
- [x] Existing M03 document ID formats are preserved:
  - `session|{guid:D}`
  - `turn|{guid:D}|{turnNumber:D3}`
- [x] Public API returns the raw interview/session id, not the Cosmos document id
- [x] Users can only list/retrieve/update their own interviews
- [x] Admin users do not receive cross-user interview access in M04
- [x] Anonymous users receive `401`
- [x] Authenticated non-invited users receive `403`
- [x] Invited users can create/list/retrieve/answer/complete their own interviews
- [x] Invited users can start their own created interviews
- [x] Unit tests cover question generator behavior
- [x] Unit tests cover interview state transitions
- [x] Integration tests cover endpoint happy path
- [x] Integration tests cover authorization scenarios
- [x] Existing tests continue to pass

## Tasks

### [ ] Interview persistence/query support

- [x] Define interview API request/response DTOs
- [x] Add hardcoded question generator
- [x] Add interview state constants: `created`, `active`, `completed`
- [x] Add interview service/state handling
- [x] Add persistence access for listing sessions and loading session turns
- [x] Add unit tests for state transitions

### [ ] Interview API endpoint implementation

- [x] Implement `POST /api/interviews`
- [x] Implement `POST /api/interviews/{id}/start`
- [x] Implement `GET /api/interviews`
- [x] Implement `GET /api/interviews/{id}`
- [x] Implement `POST /api/interviews/{id}/answers`
- [x] Implement `POST /api/interviews/{id}/complete`

### [ ] Interview API tests and documentation

- [x] Add integration tests for interview API happy path
- [x] Add integration tests for authorization scenarios
- [x] Update Swagger/OpenAPI documentation if applicable

## Verification

- [x] `POST /api/interviews` creates a persisted session document
- [x] `POST /api/interviews` creates session in `created` status without creating turn 1
- [x] `POST /api/interviews/{id}/start` creates turn 1 with a stubbed question and moves status to `active`
- [x] Created session document uses id format `session|{guid:D}`
- [x] Created turn document uses id format `turn|{guid:D}|001`
- [x] `GET /api/interviews` returns only the current user’s interviews
- [x] `GET /api/interviews?status=created` returns only created interviews
- [x] `GET /api/interviews?status=active` returns only active interviews
- [x] `GET /api/interviews?status=completed` returns only completed interviews
- [x] `GET /api/interviews?status=created&status=active` returns interviews matching any selected status
- [x] `GET /api/interviews/{id}` returns the persisted current question for active interviews without generating new turns
- [x] `GET /api/interviews/{id}` does not return full turn/answer history in M04
- [x] `POST /api/interviews/{id}/answers` saves the submitted answer
- [x] `POST /api/interviews/{id}/answers` creates the next turn when questions remain
- [x] Final answer marks interview as completed
- [x] Duplicate answer submission is rejected
- [x] Wrong or stale `turnNumber` is rejected
- [x] Answer submission after completion is rejected
- [x] Completing an already completed interview is rejected
- [x] Anonymous requests return `401`
- [x] Non-invited authenticated requests return `403`
- [x] Invited user can complete the full API flow
- [x] User cannot access another user’s interview
- [x] Full test suite passes

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
POST /api/interviews/{id}/start
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
created
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
