# 01c - CI/CD pipeline

Phase: 1
Milestone: 01 - Foundation and Azure skeleton
Type: Feature
Status: Planned

## Summary

Create GitHub Actions workflows for CI validation and CD deployment of infrastructure and application.

## Problem and User Value

Without CI/CD, quality checks and deployments are manual, inconsistent, and error-prone.

This feature provides reliable validation and repeatable deployment from source control.

## Scope

- Implement CI workflow for pull requests
- Implement CD workflow for infrastructure and app deployment
- Configure Azure authentication using OIDC in workflows
- Add deployment verification in CD

## Out of Scope

- Advanced multi-environment release gates
- Canary or blue-green deployment strategy
- Performance and load testing pipelines

## Traceability

- Phase: 1
- Milestone: 01 - Foundation and Azure skeleton
- Related ADRs: ADR 0001, ADR 0007 in `docs/decisions.md`
- Related docs: `docs/roadmap.md`, `docs/milestones.md`
- Requirement IDs: Platform foundation (no direct end-user FR mapping)

Acceptance Criteria:

- [ ] GitHub Actions CI workflow runs build and test checks
- [ ] GitHub Actions CD workflow can deploy infrastructure and app
- [ ] Workflow authentication to Azure uses OIDC
- [ ] CD includes post-deployment verification step

## Sub-Issues

- [ ] Task: Create CI workflow for pull requests
- [ ] Task: Add web build and API build steps to CI
- [ ] Task: Create CD workflow for main branch or manual dispatch
- [ ] Task: Add Azure login with OpenID Connect in workflows
- [ ] Task: Add deployment verification step after CD

## Verification

- [ ] CI runs automatically on pull request and reports pass or fail
- [ ] CD can deploy infra and app end-to-end
- [ ] Deployment verification confirms app health endpoint response

## Dependencies and Blockers

Blocked by:

- 01a - Workspace and project skeleton
- 01b - Infrastructure as code baseline

Blocks:

- 02a - API authentication core
- 01d - Foundation API and service validation
- 03a - Cosmos DB persistence baseline

## Risks and Open Questions

- Risk: OIDC or Azure permission misconfiguration can block deployment
- Risk: CI runtime drift between local and pipeline environments
