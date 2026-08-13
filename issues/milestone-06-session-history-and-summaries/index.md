# Milestone 06 - Session history and summaries (placeholder)

Epic type: Milestone

## Overview

Let users review completed interviews in full and read an AI-generated final summary per completed session. This is primarily a read/aggregation milestone: the data it displays (per-turn questions, answers, per-dimension scores, feedback) was already produced and persisted in M04 and M05.

M04 deliberately kept `GET /api/interviews/{id}` shallow — it did not return full turn/answer history. M06 adds the detail read path, generates a one-time summary when a session completes, and builds the history and detail UI.

## Feature Issues

- 06a - Session detail read API (full turn history)
- 06b - Session summary generation (AI, completed sessions)
- 06c - History and session detail UI
- 06d - End-to-end verification and documentation

## Key Decisions

- **Read path, not new writes**: M06 surfaces data M04/M05 already persisted. It must not re-run evaluation to display scores/feedback — it reads the stored per-dimension scores.
- **Detail endpoint**: dedicated `GET /api/interviews/{id}/detail`; the existing `GET /api/interviews/{id}` remains the shallow resume/status endpoint and gains `aggregateScore int?` (null for non-completed).
- **Detail read is a query, not a point read**: loading a full session (session + ordered turns) uses a dedicated query class, not the point-read `IRepository<T>`. Queries are scoped to the user's partition.
- **`InterviewFeedback` refactor (first task in 06b)**: the existing `InterviewFeedback(int Score, string? Summary)` domain type is split into two separate session properties:
  - `SessionResult(int OverallScore)` — deterministic aggregate, stored after every answer
  - `InterviewSummary(string Text, DateTimeOffset CreatedAt)` — AI narrative, best-effort
  - `AiCallMetadata? SummaryMetadata` — prompt version and provider metadata, following the M05 pattern (`summaryAi` on `CosmosSessionDocument`)
- **Score stored after every answer**: `SubmitAnswer` computes `SessionResult` from in-memory turns (already loaded for AI context) and saves it on the session document on every answer. This means early completion (`POST /api/interviews/{id}/complete`) never needs to load turns — the score is already current.
- **Score for early-completed sessions with no answers**: `SessionResult` is null when no turns have been answered.
- **`aggregateScore` on `InterviewResponse`**: additive field, null for `Created`/`Active`, populated for `Completed`. List and shallow reads both return it without loading turns.
- **Summary is generated once, on completion**: a completed session gets one persisted summary. It reuses the M05 AI boundary (prompt versioning, response validation, graceful failure). Viewing history never triggers a new AI call.
- **Summary failure is non-fatal**: if summary generation fails, the session is still completed and reviewable with its score; the summary can be regenerated via `POST /api/interviews/{id}/summary`. Completion is never blocked by summary generation.
- **Persistence unchanged**: summary and result are stored on the existing `CosmosSessionDocument`; turn history reads from `CosmosTurnDocument`. No new container.
- **Prompt version in AI metadata, not in summary**: consistent with M05 pattern:
  ```
  turn.QuestionGenerationMetadata → questionAi.promptVersion
  turn.AnswerEvaluationMetadata   → evaluationAi.promptVersion
  session.SummaryMetadata         → summaryAi.promptVersion
  ```

## Read Shape

Detail read (`GET /api/interviews/{id}/detail`) returns the full reviewable session:

```text
session:
  setup (role, seniority, type, focusArea)
  status, completedAt
  aggregateScore          ← null when 0 answers, integer average otherwise
  summary?                ← { text, createdAt } when present
  summaryAi?              ← { promptVersion, provider } when present

turns (ordered by turnNumber):
  question    { text, topic }
  answer      { text }
  evaluation  { overallScore, feedback, dimensions[] }
  questionAi  { promptVersion }
  evaluationAi { promptVersion }
```

## Exit Criteria

- All 4 features shipped and merged
- `GET /api/interviews/{id}/detail` returns full turn history for any session
- `GET /api/interviews/{id}` and list endpoint include `aggregateScore` for completed sessions
- Completed sessions carry a persisted AI summary
- History page lists a user's completed sessions
- Session detail page shows questions, answers, per-dimension scores, and feedback
- Viewing history triggers no AI calls
- Summary generation failure does not block completion or review
- Stub AI path keeps CI credential-free
- All existing tests pass; new unit and integration tests added
- Architecture documentation updated

## Notes

Summary content and rubric feedback come from stored M05 output. M06 aggregates and presents; it does not recompute scores.

The summary generator is a new single-purpose abstraction (e.g. `ISessionSummarizer`), separate from `IQuestionGenerator` and `IAnswerEvaluator`, consistent with the one-capability-per-interface approach.
