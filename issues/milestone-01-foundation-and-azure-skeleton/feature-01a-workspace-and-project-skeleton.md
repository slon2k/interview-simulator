# 01a - Workspace and project skeleton

Phase: 1
Milestone: 01 - Foundation and Azure skeleton
Type: Feature
Status: Planned

## Summary

Create the initial repository workspace for the web and API applications with clear project boundaries and consistent development conventions.

## Problem and User Value

Without a stable project skeleton, all follow-up features are slower, harder to review, and prone to inconsistent structure.

This feature provides the base that enables infrastructure, CI/CD, auth, and application features to be delivered predictably.

## Scope

- Scaffold Vite React frontend in `web/`
- Scaffold ASP.NET Core API in `api/`
- Establish repository-level structure conventions
- Add baseline lint and format configuration
- Ensure both projects build successfully in local development

## Out of Scope

- Cloud infrastructure provisioning
- Authentication and authorization implementation
- Cosmos DB data model and repository implementation
- Interview flow features

## Traceability

- Phase: 1
- Milestone: 01 - Foundation and Azure skeleton
- Related ADRs: ADR 0001, ADR 0007 in `docs/decisions.md`
- Related docs: `docs/roadmap.md`, `docs/milestones.md`
- Requirement IDs: Platform foundation (no direct end-user FR mapping)

## Acceptance Criteria

- [ ] Vite React frontend skeleton exists in `web/`
- [ ] ASP.NET Core API skeleton exists in `api/`
- [ ] Baseline project structure and naming conventions are documented
- [ ] Baseline lint and format configuration is present for both projects
- [ ] Local build succeeds for both web and API projects

## Sub-Issues

- [ ] Task: Scaffold Vite React app in `web/`
- [ ] Task: Scaffold ASP.NET Core API app in `api/`
- [ ] Task: Define and document repository structure conventions
- [ ] Task: Add baseline lint and format configuration
- [ ] Task: Validate local build and capture build output evidence

## Verification

- [ ] `web/` project builds successfully
- [ ] `api/` project builds successfully
- [ ] File structure matches documented conventions
- [ ] Build commands and expected outputs are documented

## Dependencies and Blockers

Blocked by:

- None

Blocks:

- 01b - Infrastructure as code baseline
- 01c - CI/CD pipeline
- 02a - API authentication core
- 01d - Foundation API and service validation
- 03a - Cosmos DB persistence baseline

## Risks and Open Questions

- Risk: Over-scaffolding early can create maintenance overhead
- Risk: Inconsistent conventions between web and API projects
- Open question: Do we want a shared contracts package in phase 1 or defer it?
