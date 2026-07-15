# Milestones

GitHub milestones are used as epics/work packages.  
Roadmap phases are tracked separately in `docs/roadmap.md` and in the GitHub Project `Phase` field.

---

## 01 - Foundation and Azure skeleton

Set up the initial repository, frontend, API project, Azure App Service deployment, infrastructure as code baseline, and basic CI/CD workflow.

Done when:

- Vite React frontend skeleton exists
- ASP.NET Core API skeleton exists
- Bicep templates exist in `infra/` for core Azure resources
- App Service deployment works
- GitHub Actions CI workflow runs build/test checks
- GitHub Actions CD workflow can deploy infrastructure and app
- `/api/health` endpoint works
- Basic local development flow is documented
- Azure OpenAI access/model deployment is validated
- Azure Speech token flow is validated

---

## 02 - Auth and invite-only access

Implement GitHub OAuth authentication in ASP.NET Core and restrict app/API usage to invited users.

Done when:

- GitHub login works
- `/api/me` returns current user info
- Invite-only policy is configured in backend authorization
- Protected routes require authenticated invited users
- API checks user authorization defensively

---

## 03 - Cosmos DB persistence

Set up Cosmos DB and implement persistence for sessions, turns, and optional user profile data.

Done when:

- Cosmos DB free tier account/container exists
- Partition key strategy is documented
- Repository abstraction is implemented
- Test read/write works from ASP.NET Core API
- Session and turn document models are defined

---

## 04 - Text interview flow

Build the core text-based interview experience.

Done when:

- User can configure interview settings
- User can start a session
- First question is generated
- User can submit text answers
- Next question is shown
- Session state is persisted

---

## 05 - AI prompts and rubric evaluation

Implement structured AI evaluation using role/interview-type-specific rubrics.

Done when:

- Evaluation JSON schema is defined
- Technical, behavioral, and system design rubrics exist
- Answer evaluation returns structured feedback
- Adaptive next question is generated
- AI response validation/error handling exists
- Prompt versions are tracked

---

## 06 - Session history and summaries

Allow users to review past sessions and generate final summaries.

Done when:

- History page exists
- Session detail page exists
- Completed sessions have summaries
- Questions, answers, scores, and feedback can be reviewed

---

## 07 - Dashboard analytics

Provide progress analytics based on stored interview sessions.

Done when:

- Dashboard summary endpoint exists
- Average score over time is shown
- Scores by topic/interview type are shown
- Weakest rubric dimensions are shown
- Recent sessions are shown

---

## 08 - Voice UX with Azure Speech

Add optional voice input/output using Azure AI Speech.

Done when:

- `/api/speech/token` endpoint exists
- Questions can be spoken aloud
- User can answer using push-to-talk
- Transcript appears in editable text area
- User can submit edited transcript
- Microphone permission errors are handled
- Recording duration is limited

---

## 09 - Observability, cost controls, and polish

Improve reliability, logging, UX polish, and cost safety.

Done when:

- Application Insights is configured
- AI operation metadata is logged safely
- Basic per-user usage limits exist
- Loading and error states are handled
- App has acceptable UI polish
- Azure budget/cost monitoring notes are documented

---

## 10 - Documentation and demo readiness

Prepare the project for portfolio/demo usage.

Done when:

- README is complete
- Architecture document is complete
- Setup instructions are documented
- Demo scenario is documented
- Screenshots/GIF/video are added
