# Repository Structure Conventions

This document defines the repository structure conventions for the Interview Simulator project.

## Goals

- Keep code ownership and folder responsibilities unambiguous.
- Prevent generated artifacts from drifting into source-controlled folders.
- Keep local development and production deployment workflows explicit.
- Maintain consistency for branch naming, issue tracking, and documentation.

## Top-Level Structure

Expected top-level folders and files:

- `api/`: ASP.NET Core backend source and tests.
- `web/`: Vite + React frontend source and tooling.
- `docs/`: architecture, ADRs, roadmap, and project conventions.
- `issues/`: milestone and feature planning artifacts.
- `infra/`: infrastructure as code and deployment infrastructure assets.
- `.github/`: GitHub workflows and templates.
- Root build/config files: `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `NuGet.Config`.

Do not add new top-level folders unless they represent a stable, long-term concern.

## Backend Conventions

- Backend application projects live under `api/src/`.
- Backend test projects live under `api/tests/`.
- Test project naming:
  - Unit tests: `<ProjectName>.UnitTests`
  - Integration tests: `<ProjectName>.IntegrationTests`
- Keep backend API routes under `/api/*`.

### Backend Static Files

- `api/src/InterviewSimulator.Api/wwwroot/` is placeholder-only in source control.
- Keep only minimal placeholder content required for runtime compatibility (for example `.gitkeep`).
- Frontend static assets are produced by publish/build pipelines and copied into publish output `wwwroot`, not authored manually in source `wwwroot`.

## Frontend Conventions

- Frontend application lives under `web/`.
- Frontend source code lives under `web/src/`.
- Feature-focused grouping is preferred in `web/src/features/`.
- Shared app composition utilities belong in `web/src/app/`.
- API client code belongs in `web/src/api/`.

### Frontend Build Artifacts

- `web/dist/` is generated output and must not be manually edited.
- `web/node_modules/` is local dependency cache and must not be committed.

## Documentation Conventions

- `docs/` contains durable technical documentation:
  - Architecture, ADRs, technical standards, and conventions.
- `issues/` contains execution planning:
  - Milestones, feature breakdowns, and work tracking artifacts.

Source-of-truth split:

- Technical decisions and standards: `docs/`.
- Planned and in-flight work items: `issues/`.

## Infrastructure Conventions

- `infra/` contains infrastructure definitions and related deployment artifacts.
- Organize `infra/` by environment or module when content grows.
- Keep application code out of `infra/`.

## Configuration and Secrets

- Commit only templates and non-secret defaults.
- Do not commit secrets to the repository.
- Use:
  - `.env.example` for frontend environment variable templates.
  - User secrets or environment variables for backend secrets.

## Local Development vs Production Hosting

Local development model:

- Run API and frontend separately.
- Frontend dev server proxies API calls (for example `/api` to backend local URL).
- Backend Development environment should not rely on built SPA static hosting.

Production model:

- Frontend is built during publish/deploy pipeline.
- Backend serves built static frontend files from publish output `wwwroot`.

## Quality Gate Conventions

Frontend:

- Keep scripts in `web/package.json`:
  - `typecheck`
  - `lint`
  - `format:check`
  - `build`
  - `check` (aggregate)

Backend:

- Use root solution/project build for compile validation.
- Keep analyzer and formatting conventions in `.editorconfig` and project settings.

## Naming and Traceability

- Project names should remain `InterviewSimulator.*` for consistency.
- Feature work should map to files in `issues/milestone-*/`.
- Branch naming should reference tracked work items (for example `6-task-add-baseline-lint-and-format-configuration`).

## Change Management Rules

- Do not hand-edit generated folders (`bin`, `obj`, `dist`, `publish`).
- Prefer additive documentation updates over silent structural changes.
- If introducing a new structural pattern, update this document in the same PR.
