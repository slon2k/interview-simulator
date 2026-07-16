# Interview Simulator

Interview Simulator is an AI-powered interview practice application designed to help users train for technical, behavioral, and role-specific interviews.

Users can configure an interview by selecting a role, seniority level, topic, and interview type. The system generates adaptive questions using Azure OpenAI, evaluates answers with structured rubrics, provides actionable feedback, and stores session history for later review.

The project is built as a portfolio and learning project focused on practical AI integration with Azure services.

## Planned Features

- Role/topic/seniority-based interview setup
- Adaptive AI-generated interview questions
- Text-based answer submission
- Rubric-based answer evaluation
- Session summaries and history
- Progress dashboard
- Voice input with Azure Speech-to-Text
- Voice output with Azure Text-to-Speech
- Invite-only access using ASP.NET Core cookie auth with GitHub OAuth
- Session-centric API design (`/api/sessions/*`)

## Tech Stack

- Vite + React + TypeScript
- Azure App Service
- ASP.NET Core Web API
- Azure Cosmos DB
- Azure OpenAI
- Azure AI Speech
- GitHub OAuth authentication with secure cookie sessions (Entra ID optional later)

## Publish Frontend + API Together

The API publish process builds the Vite frontend and places its output into publish `wwwroot` automatically.

```powershell
dotnet publish api/src/InterviewSimulator.Api/InterviewSimulator.Api.csproj -c Release
```

After publish, the generated output folder contains both backend binaries and frontend static files for production hosting.
