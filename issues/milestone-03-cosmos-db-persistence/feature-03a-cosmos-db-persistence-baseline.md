# 03a - Cosmos DB IaC and configuration baseline

Phase: 1
Milestone: 03 - Cosmos DB persistence
Type: Feature
Status: Planned

## Summary

Provision Cosmos DB infrastructure through Bicep and configure App Service runtime settings to use Cosmos DB with managed identity and RBAC.

## Problem and User Value

The application needs a durable persistence service before interview sessions, history, and analytics can be implemented.

This feature establishes the Cosmos DB infrastructure and runtime configuration foundation without implementing application persistence behavior yet.

## Scope

- Provision Cosmos DB account through Bicep
- Provision Cosmos DB SQL database through Bicep
- Provision Cosmos DB SQL container through Bicep
- Define initial container partition key path
- Configure App Service Cosmos DB app settings
- Grant App Service managed identity Cosmos DB data-plane access
- Ensure Cosmos DB keys are not used by application runtime
- Document Cosmos DB infrastructure and runtime configuration

## Out of Scope

- Session and turn document contracts
- Repository abstraction
- Cosmos SDK integration in API code
- API read and write smoke endpoint
- Dashboard queries
- Interview session persistence behavior
- Advanced indexing tuning
- Data migrations

## Acceptance Criteria

- [ ] Cosmos DB account is provisioned through Bicep
- [ ] Cosmos DB SQL database is provisioned through Bicep
- [ ] Cosmos DB SQL container is provisioned through Bicep
- [ ] Free tier is enabled for dev when available, or fallback behavior is documented
- [ ] Container partition key path is /userId
- [ ] Initial indexing policy is explicit, or default behavior is documented
- [ ] App Service managed identity is granted Cosmos DB data-plane access using Cosmos DB Built-in Data Contributor at least-privilege scope, with scope rationale documented
- [ ] Cosmos DB account keys are not used by the application
- [ ] App Service receives Cosmos DB configuration values:
  - CosmosDb\_\_Enabled
  - CosmosDb\_\_Endpoint
  - CosmosDb\_\_DatabaseName
  - CosmosDb\_\_ContainerName
  - CosmosDb\_\_UseManagedIdentity
- [ ] CosmosDb\_\_Enabled remains false until 03b is merged so app startup and health remain stable without repository integration
- [ ] Bicep outputs include non-secret Cosmos DB metadata only
- [ ] Infra deployment pipeline succeeds for dev
- [ ] Cosmos DB configuration and troubleshooting are documented

## Sub-Issues

- [ ] Task: Add Cosmos DB Bicep module for account, SQL database, and container
- [ ] Task: Add Cosmos DB SQL data-plane RBAC role assignment for App Service managed identity
- [ ] Task: Wire Cosmos DB app settings into App Service module
- [ ] Task: Add Cosmos DB outputs to main Bicep
- [ ] Task: Document Cosmos DB configuration and troubleshooting

## Verification

- [ ] Cosmos DB account is visible in Azure
- [ ] Cosmos DB SQL database is visible in Azure
- [ ] Cosmos DB SQL container is visible in Azure
- [ ] Container partition key path is /userId
- [ ] App Service configuration contains Cosmos DB non-secret settings
- [ ] App Service managed identity has Cosmos DB data-plane role assignment
- [ ] No Cosmos DB account key is present in App Service configuration, GitHub secrets, or repository
- [ ] Infra deployment pipeline completes successfully
- [ ] /api/health still succeeds after deployment

## Dependencies and Blockers

Blocked by:

- 01b - Infrastructure as code baseline
- 01c - CI/CD pipeline

Blocks:

- 03b - Cosmos DB repository baseline
- Phase 2 interview flow features
- Session history and dashboard features

## Risks and Open Questions

Risks:

- Risk: Cosmos DB free tier may be unavailable because only one free-tier account is allowed per subscription
- Risk: Cosmos DB data-plane RBAC is different from Azure management-plane RBAC and can be misconfigured
- Risk: Cosmos DB role assignment deployment may require additional permissions for the infra pipeline identity
- Risk: Wrong partition key selection can be expensive to change later

Open questions:

- Should dev use free tier if available, or always use provisioned or manual throughput?
- Should the baseline use default indexing or an explicit minimal indexing policy?
- Should Cosmos DB account use serverless or provisioned throughput in dev?
