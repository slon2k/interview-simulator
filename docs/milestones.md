# Milestones

GitHub milestones are used as epics/work packages.  
Roadmap phases are tracked separately in `docs/roadmap.md` and in the GitHub Project `Phase` field.

---

## 01 - Foundation and Azure skeleton

Set up the initial repository, frontend, API project, Azure App Service deployment, infrastructure as code baseline, and basic CI/CD workflow.

Acceptance Criteria:

- [ ] Vite React frontend skeleton exists
- [ ] ASP.NET Core API skeleton exists
- [ ] Bicep templates exist in `infra/` for core Azure resources
- [ ] App Service deployment works
- [ ] GitHub Actions CI workflow runs build/test checks
- [ ] GitHub Actions CD workflow can deploy infrastructure and app
- [ ] `/api/health` endpoint works
- [ ] Basic local development flow is documented
- [ ] Azure OpenAI access/model deployment is validated
- [ ] Azure Speech token flow is validated

---

## 02 - Auth and invite-only access

Implement authentication and invite-only access across backend and frontend UX.

Acceptance Criteria:

- [ ] API authentication core is implemented with GitHub OAuth and cookie session handling
- [ ] GitHub numeric user id is captured and normalized as `github|{githubUserId}`
- [ ] Config-based admin/invited allowlist exists
- [ ] Invite-only policy is configured in backend authorization
- [ ] Admin users are treated as invited users
- [ ] `/api/me` returns current user info including `isAuthenticated`, `isInvited`, and `isAdmin`
- [ ] Protected routes require authenticated invited users
- [ ] Non-invited authenticated users receive a clear forbidden response/page
- [ ] API checks user authorization defensively
- [ ] Frontend login and logout UX is implemented and integrated with backend auth flow
- [ ] Anonymous protected API requests return `401` JSON and non-invited authenticated requests return `403` JSON instead of unexpected HTML redirects.

---

## 03 - Cosmos DB persistence

### Status: 03a & 03b Complete, 03c In Progress

Delivered Cosmos DB in layers: infrastructure and runtime configuration (03a/03b), now adding session/turn document models (03c).

Acceptance Criteria:

- [x] Cosmos DB account, SQL database, and SQL container are provisioned through Bicep
- [x] App Service managed identity has Cosmos DB data-plane access, and account keys are not used by the app
- [x] App Service receives non-secret Cosmos DB configuration values
- [x] Cosmos DB SDK integration and validated CosmosDb options are implemented in API
- [ ] Session and turn document models are defined with deterministic ID strategy
- [ ] Repository abstraction and Cosmos DB repository implementation are in place ✅ (beyond scope)
- [ ] User persistence on OAuth login is working ✅ (out-of-scope implementation)
- [ ] API-level read/write smoke check works in dev with invite-only authorization (in progress: 03c)
- [ ] Normal CI does not require live Cosmos DB credentials ✅

### Summary

- **03a**: Cosmos DB IaC, configuration, managed identity ✅ Complete
- **03b**: SDK integration, repository abstraction, options validation ✅ Complete
- **03c**: Session/turn models, deterministic IDs, smoke tests (new issue)
- **Bonus**: User persistence middleware for OAuth login ✅ Implemented (closes authentication gap)

---

## 04 - Text interview flow

Build the core text-based interview experience with stub question generation. M05 replaces stubs with real AI.

Acceptance Criteria:

- [ ] POST /api/interviews creates an interview in `created` status with deterministic ID, persists to Cosmos
- [ ] POST /api/interviews/{id}/start transitions interview to `active` and creates first turn with stubbed question
- [ ] GET /api/interviews returns list of user's interviews with status filter (`created`, `active`, `completed`) and multi-status support
- [ ] GET /api/interviews/{id} retrieves interview state by ID with authenticated userId partition key
- [ ] POST /api/interviews/{id}/answers saves answer, returns stubbed next question
- [ ] POST /api/interviews/{id}/complete marks interview `completed`, sets completedAt
- [ ] /interviews page lists user interviews with resume/view links (supports status filtering)
- [ ] /interviews/new page shows setup form (role/topic/seniority/type) → POST /api/interviews → start flow → active interview
- [ ] /interviews/{id} page displays active interview (question, answer input, next/complete buttons)
- [ ] All endpoints require invite-only authorization
- [ ] Anonymous users receive 401, non-invited authenticated users receive 403
- [ ] Integration test covers happy path: setup → answer 3 questions → complete
- [ ] All existing tests continue to pass
- [ ] Architecture documentation updated

---

## 05 - AI prompts and rubric evaluation

Implement structured AI evaluation using role/interview-type-specific rubrics.

Acceptance Criteria:

- [ ] Evaluation JSON schema is defined
- [ ] Technical, behavioral, and system design rubrics exist
- [ ] Answer evaluation returns structured feedback
- [ ] Adaptive next question is generated
- [ ] AI response validation/error handling exists
- [ ] Prompt versions are tracked

---

## 06 - Session history and summaries

Allow users to review past sessions and generate final summaries.

Acceptance Criteria:

- [ ] History page exists
- [ ] Session detail page exists
- [ ] Completed sessions have summaries
- [ ] Questions, answers, scores, and feedback can be reviewed

---

## 07 - Dashboard analytics

Provide progress analytics based on stored interview sessions.

Acceptance Criteria:

- [ ] Dashboard summary endpoint exists
- [ ] Average score over time is shown
- [ ] Scores by topic/interview type are shown
- [ ] Weakest rubric dimensions are shown
- [ ] Recent sessions are shown

---

## 08 - Voice UX with Azure Speech

Add optional voice input/output using Azure AI Speech.

Acceptance Criteria:

- [ ] `/api/speech/token` endpoint exists
- [ ] Questions can be spoken aloud
- [ ] User can answer using push-to-talk
- [ ] Transcript appears in editable text area
- [ ] User can submit edited transcript
- [ ] Microphone permission errors are handled
- [ ] Recording duration is limited

---

## 09 - Observability, cost controls, and polish

Improve reliability, logging, UX polish, and cost safety.

Acceptance Criteria:

- [ ] Application Insights is configured
- [ ] AI operation metadata is logged safely
- [ ] Basic per-user usage limits exist
- [ ] Loading and error states are handled
- [ ] App has acceptable UI polish
- [ ] Azure budget/cost monitoring notes are documented

---

## 10 - Documentation and demo readiness

Prepare the project for portfolio/demo usage.

Acceptance Criteria:

- [ ] README is complete
- [ ] Architecture document is complete
- [ ] Setup instructions are documented
- [ ] Demo scenario is documented
- [ ] Screenshots/GIF/video are added
