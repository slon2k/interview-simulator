# Deployment Guide (Azure + GitHub Actions)

This guide covers setup and execution for:

- [ .github/workflows/deploy-infra-dev.yml](../.github/workflows/deploy-infra-dev.yml)
- [ .github/workflows/deploy-app-dev.yml](../.github/workflows/deploy-app-dev.yml)

## Prerequisites

- Azure subscription and target resource group
- GitHub repository with a `dev` environment
- Access to configure Microsoft Entra app registrations and Azure RBAC

## Azure Setup

1. Create (or reuse) a Microsoft Entra app registration for GitHub Actions.
2. Add a federated credential to the app registration.
   - Issuer: `https://token.actions.githubusercontent.com`
   - Audience: `api://AzureADTokenExchange`
   - Subject: `repo:<owner>/<repo>:environment:dev`
3. Grant Contributor role to the service principal on the target resource group.

Example CLI commands:

```powershell
# Resolve service principal object id from app/client id.
$spObjectId = az ad sp show --id <AZURE_CLIENT_ID> --query id -o tsv

# Grant Contributor on the deployment resource group.
az role assignment create \
  --assignee-object-id $spObjectId \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope /subscriptions/<AZURE_SUBSCRIPTION_ID>/resourceGroups/<AZURE_RESOURCE_GROUP>
```

## GitHub Setup

Create environment `dev` and add the following values.

Variables required by infra deployment workflow:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_RESOURCE_GROUP`

Variables required by app deployment workflow:

- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_URL`
- `NODE_VERSION` (optional, defaults to `24.x`)

Secrets required by app deployment workflow:

- `AZURE_WEBAPP_PUBLISH_PROFILE` (publish profile XML from App Service)

## Deployment Order

1. Run infrastructure deployment workflow first.
2. Read deployment outputs and set:
   - `AZURE_WEBAPP_NAME` from `webAppName`
   - `AZURE_WEBAPP_URL` from `webAppUrl`
3. Add `AZURE_WEBAPP_PUBLISH_PROFILE` secret.
4. Run app deployment workflow.

## Environment Mode Notes

The infrastructure sets App Service configuration to:

- `ASPNETCORE_ENVIRONMENT=Production` for runtime/host mode
- `APP_ENVIRONMENT=dev|test|prod` for application-specific behavior

This separates host behavior from app-level environment logic.

## Troubleshooting

- Login failures during `azure/login`:
  - Verify OIDC federated credential subject exactly matches `repo:<owner>/<repo>:environment:dev`.
- Authorization failures during `az deployment group create`:
  - Verify Contributor role assignment on the target resource group.
- Missing workflow configuration errors:
  - Ensure all required GitHub variables and secrets are present in environment `dev`.
- App deployment succeeds but site behavior is wrong:
  - Validate App Service settings include `ASPNETCORE_ENVIRONMENT=Production` and expected `APP_ENVIRONMENT`.
