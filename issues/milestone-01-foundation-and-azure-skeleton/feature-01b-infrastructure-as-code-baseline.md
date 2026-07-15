# 01b - Infrastructure as code baseline

Phase: 1
Milestone: 01 - Foundation and Azure skeleton
Type: Feature
Status: Planned

## Summary

Provision core Azure resources with Bicep templates so the platform is reproducible and deployable from source control.

## Problem and User Value

Without infrastructure as code, environments drift and deployments depend on manual portal work.

This feature provides repeatable infrastructure provisioning for the rest of phase 1 work.

## Scope

- Define Bicep templates in `infra/` for core resources
- Support parameterized deployment for development environment
- Document deployment command flow for infra

## Out of Scope

- Full production hardening and policy enforcement
- Multi-region and disaster recovery design
- Application-level business features

## Traceability

- Phase: 1
- Milestone: 01 - Foundation and Azure skeleton
- Related ADRs: ADR 0001, ADR 0007 in `docs/decisions.md`
- Related docs: `docs/roadmap.md`, `docs/milestones.md`, `docs/architecture.md`
- Requirement IDs: Platform foundation (no direct end-user FR mapping)

Acceptance Criteria:

- [ ] Bicep templates exist in `infra/` for core Azure resources
- [ ] App Service deployment works
- [ ] Infra parameters are defined for development deployment
- [ ] Infra deployment commands are documented

## Sub-Issues

- [ ] Task: Create Bicep template for App Service plan and web app
- [ ] Task: Create Bicep template for Application Insights
- [ ] Task: Create Bicep template for Cosmos DB account, database, and container
- [ ] Task: Add parameter files for development environment
- [ ] Task: Add deployment script or command documentation for infra deployment

## Verification

- [ ] Bicep templates compile without errors
- [ ] Development deployment completes successfully
- [ ] Provisioned resources match expected naming and shape

## Dependencies and Blockers

Blocked by:

- 01a - Workspace and project skeleton

Blocks:

- 01c - CI/CD pipeline
- 01d - Foundation API and service validation
- 03a - Cosmos DB persistence baseline

## Risks and Open Questions

- Risk: Environment-specific parameters may drift between local and CI
- Risk: Under-specified resource settings can cause later deployment refactors
