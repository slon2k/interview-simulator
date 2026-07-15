# ADR 0001: Initial Architecture

## Status

Proposed

## Context

The project is a personal/portfolio AI-powered interview simulator. The main goals are realistic interview practice, Azure AI integration experience, and low operational cost.

The app should support invite-only access, text-based interview sessions, AI-generated adaptive questions, rubric-based evaluation, session history, dashboard analytics, and later voice input/output.

## Decision

Use the following architecture:

- React + TypeScript frontend
- Azure Static Web Apps for frontend hosting
- Azure Static Web Apps built-in authentication
- GitHub or Entra ID login
- Invite-only access using custom SWA role
- Azure Functions with .NET isolated worker for backend API
- Azure Cosmos DB for persistence
- Azure OpenAI for question generation, evaluation, and summaries
- Azure AI Speech for speech-to-text and text-to-speech
- Application Insights for monitoring

## Consequences

### Benefits

- Low cost
- Low backend ceremony
- No custom authentication implementation
- Good fit for personal/invite-only usage
- Azure-native architecture
- Easy deployment through GitHub Actions
- Allows focus on AI features

### Trade-offs

- Azure Functions are less conventional than ASP.NET Core Web API
- SWA authentication is platform-specific
- Advanced backend authorization scenarios may be less flexible
- React and .NET require duplicated DTOs unless generated
- Voice UX will rely on browser-side Azure Speech SDK

## Alternatives Considered

- ASP.NET Core Web API with ASP.NET Core Identity
- Blazor WebAssembly
- Blazor Server
- Azure Static Web Apps with separate App Service backend
