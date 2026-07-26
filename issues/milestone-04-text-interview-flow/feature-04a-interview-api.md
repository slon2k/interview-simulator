# 04a - Interview API

Phase: 2
Milestone: 04 - Text interview flow
Type: Feature
Status: Planned

## Summary

Implement core interview CRUD operations and list endpoint. Covers session lifecycle: create, retrieve, list (with status filter), submit answers, and complete. Questions are stubbed with a hardcoded placeholder. All endpoints validate invite-only authorization.

## Problem and User Value

M03 delivered the persistence foundation (Cosmos DB repository, session/turn models). Now M04 adds the HTTP API and state machine to make interviews work end-to-end. This feature provides the backend for setup form, active interview UI, and resume/list pages.

## Scope

- Implement `IQuestionGenerator` interface with stubbed hardcoded implementation
- Create `InterviewService` or similar domain service encapsulating interview state machine
- Implement `POST /api/interviews` to create interview, persist to Cosmos, return first question (stubbed)
- Implement `GET /api/interviews` to list user's interviews with optional `?status=` filter (`inProgress`, `completed`, `all`)
- Implement `GET /api/interviews/{id}` to retrieve interview by ID
- Implement `POST /api/interviews/{id}/answers` to save answer turn, return next question (stubbed)
- Implement `POST /api/interviews/{id}/complete` to mark interview done
- Add request/response DTOs for all endpoints
- Add unit tests for question generator stub and state transitions
- Add integration tests for all endpoints with auth scenarios (anonymous, non-invited, invited, admin)
- Document API contract (OpenAPI/Swagger)

## Out of Scope

- Real AI question generation (M05)
- Answer evaluation or scoring
- Prompt versioning
- Error recovery or retries for OpenAI

## Acceptance Criteria

- [ ] `IQuestionGenerator` interface exists with one method: `GenerateQuestion(topic, role, seniority) -> string`
- [ ] `HardcodedQuestionGenerator` implements `IQuestionGenerator` with hardcoded questions by topic
- [ ] POST `/api/interviews` accepts `{ role, seniority, topic, interviewType, questionCount }`
- [ ] POST `/api/interviews` creates `CosmosInterviewDocument`, persists to Cosmos, returns `{ id, status, question, createdAt }`
- [ ] GET `/api/interviews` returns `{ interviews: [...], count }` filtered by optional `?status=`
- [ ] GET `/api/interviews/{id}` returns full interview state including all turns, questions, answers
- [ ] POST `/api/interviews/{id}/answers` accepts `{ text }`, saves turn to Cosmos, returns next question
- [ ] POST `/api/interviews/{id}/complete` sets `status=completed`, `completedAt=now`
- [ ] All endpoints require authenticated invited user (401 anonymous, 403 non-invited)
- [ ] Admin users can list/retrieve any user's interviews (or admin-only endpoint) — TBD
- [ ] Interview state transitions validated (e.g., can't complete twice, can't answer after complete)
- [ ] Unit tests cover question generator and state transitions
- [ ] Integration tests cover all endpoints with auth scenarios
- [ ] All existing tests continue to pass
- [ ] Swagger/OpenAPI updated

## Sub-Issues

- [ ] Task: Define `IQuestionGenerator` interface and `HardcodedQuestionGenerator`
- [ ] Task: Create `InterviewService` domain service with state machine logic
- [ ] Task: Implement POST /api/interviews endpoint with Cosmos persistence
- [ ] Task: Implement GET /api/interviews endpoint with status filtering
- [ ] Task: Implement GET /api/interviews/{id} endpoint
- [ ] Task: Implement POST /api/interviews/{id}/answers endpoint
- [ ] Task: Implement POST /api/interviews/{id}/complete endpoint
- [ ] Task: Add DTOs for requests and responses
- [ ] Task: Add unit tests for question generator and service logic
- [ ] Task: Add integration tests for all endpoints with auth scenarios
- [ ] Task: Update Swagger/OpenAPI documentation

## Verification

- [ ] `HardcodedQuestionGenerator` returns same question for same topic (deterministic)
- [ ] POST /api/interviews creates document with `id=interview|{guid}` format (or similar deterministic)
- [ ] GET /api/interviews?status=inProgress returns only interviews with `status=inProgress`
- [ ] POST /api/interviews/{id}/answers increments turn count and returns next question
- [ ] Anonymous user GET /api/interviews returns 401
- [ ] Non-invited authenticated user GET /api/interviews returns 403
- [ ] Invited user GET /api/interviews returns 200 with their interviews
