# Roadmap

This roadmap describes the main delivery phases for the AI Interview Simulator.

GitHub milestones are used as epics/work packages.  
GitHub Project phases are used to group work into larger product delivery stages.

---

## Phase 1 - Foundation

Goal: Validate the main architecture and establish the Azure-based project skeleton.

### Scope

- Repository structure
- Vite React frontend skeleton
- ASP.NET Core API skeleton
- Infrastructure as code baseline in `infra/` using Bicep
- GitHub Actions CI pipeline for build/test checks
- GitHub Actions CD pipeline for Azure deployment
- Azure App Service deployment
- GitHub OAuth authentication in ASP.NET Core
- Invite-only access using backend authorization policy
- `/api/health` endpoint
- `/api/me` endpoint
- Cosmos DB free tier setup
- Initial Cosmos DB document model
- Azure OpenAI access validation
- Azure Speech token flow validation
- Local development setup documentation

### Related Milestones

- 01 - Foundation and Azure skeleton
- 02 - Auth and invite-only access
- 03 - Cosmos DB persistence

### Exit Criteria

- App is deployed to Azure App Service
- Core Azure resources can be provisioned from Bicep templates
- CI/CD pipeline can deploy infrastructure and application from GitHub Actions
- Invited user can log in
- `/api/me` returns authenticated user info
- API can read/write a test document in Cosmos DB
- API can call Azure OpenAI successfully
- API can issue an Azure Speech token
- Initial architecture is documented

---

## Phase 2 - Text Interview MVP

Goal: Build the core useful product: a text-based AI interview simulator with feedback, history, and basic progress tracking.

### Scope

- Interview setup page
- Role/topic/seniority/interview type selection
- Text-based interview session flow
- AI-generated first question
- User text answer submission
- Adaptive next questions
- Rubric-based answer evaluation
- Structured AI feedback
- Prompt versioning
- AI response validation/error handling
- Final session summary
- Session history
- Session detail page
- Basic dashboard/progress summary
- Basic loading and error states

### Basic Dashboard Scope

- Total completed sessions
- Average score
- Recent sessions
- Score trend over recent sessions
- Weakest rubric dimensions

### Related Milestones

- 04 - Text interview flow
- 05 - AI prompts and rubric evaluation
- 06 - Session history and summaries
- 07 - Dashboard analytics

### Exit Criteria

- User can complete a full text-based interview
- Each answer receives structured rubric feedback
- Session is saved in Cosmos DB
- Completed session has a final summary
- User can review past sessions
- User can see basic progress information
- App is usable by invited users without voice

---

## Phase 3 - Voice Interview and Demo Readiness

Goal: Add optional voice interaction and prepare the project for portfolio/demo usage.

### Scope

- Azure Speech SDK integration in React
- Text-to-speech for AI questions
- Replay question button
- Push-to-talk speech-to-text
- Live/interim transcript display
- Editable transcript before submission
- Recording timer and max answer duration
- Microphone permission error handling
- Application Insights integration
- Safe AI operation logging
- Basic per-user usage limits
- Cost-control notes
- UI polish
- README completion
- Architecture documentation
- Demo script
- Screenshots/GIF/video

### Related Milestones

- 08 - Voice UX with Azure Speech
- 09 - Observability, cost controls, and polish
- 10 - Documentation and demo readiness

### Exit Criteria

- User can listen to AI questions
- User can answer using voice
- Transcript can be edited before submission
- Text interview flow still works without voice
- App has basic observability and cost controls
- Project is portfolio/demo-ready

---

## Stretch / Future Ideas

These items are not required for the initial version.

- Advanced dashboard analytics
- AI-generated long-term learning plan
- Question bank management
- Custom admin panel
- Entra ID-based access as an additional provider
- Full real-time voice conversation
- SignalR streaming
- Camera/video recording
- Emotion detection
- Payment/subscription logic
