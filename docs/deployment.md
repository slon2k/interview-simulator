# Deployment Guide (Azure + GitHub Actions)

This guide covers setup and execution for:

- [.github/workflows/deploy-infra-dev.yml](../.github/workflows/deploy-infra-dev.yml)
- [.github/workflows/deploy-app-dev.yml](../.github/workflows/deploy-app-dev.yml)

## Prerequisites

- Azure subscription and target resource group
- GitHub repository with a `dev` environment
- Access to configure Microsoft Entra app registrations and Azure RBAC

## Azure Setup

Create or reuse a Microsoft Entra app registration for GitHub Actions.

Add a federated credential to the app registration with these values:

- `Issuer`: `https://token.actions.githubusercontent.com`
- `Audience`: `api://AzureADTokenExchange`
- `Subject`: `repo:<owner>/<repo>:environment:dev`

Grant the required roles to the service principal on the target resource group.

- `Contributor`
- `User Access Administrator`

Why both roles are required:

- `Contributor` allows the workflow to create and update normal resources.
- `User Access Administrator` allows the workflow to create RBAC assignments.
- This is required because the Key Vault module creates `Microsoft.Authorization/roleAssignments` for secret access.

CLI sample for steps 1-2 (create app, service principal, and federated credential):

```powershell
# Required input values (edit once).
$repoOwner = "<GITHUB_OWNER>"
$repoName = "<GITHUB_REPO>"
$environmentName = "dev"
$appRegistrationName = "gh-interview-sim-dev"

$subject = "repo:{0}/{1}:environment:{2}" -f $repoOwner, $repoName, $environmentName

# Create app registration and capture ids.
$app = az ad app create --display-name $appRegistrationName --query "{appId:appId,objectId:id}" -o json | ConvertFrom-Json

# Create service principal for the app registration.
az ad sp create --id $app.appId | Out-Null

# Create federated credential used by GitHub OIDC.
$federatedCredential = @{
  name = "gh-$environmentName"
  issuer = "https://token.actions.githubusercontent.com"
  subject = $subject
  audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json -Depth 5

$credentialFile = Join-Path $PWD "federated-credential.json"
$federatedCredential | Set-Content -Path $credentialFile -Encoding utf8

az ad app federated-credential create --id $app.objectId --parameters "@$credentialFile"

Write-Host "AZURE_CLIENT_ID=$($app.appId)"
Remove-Item $credentialFile -ErrorAction SilentlyContinue
```

PowerShell commands for step 3 (grant and verify RBAC):

```powershell
# Required input values (edit once).
$azureClientId = "<AZURE_CLIENT_ID>"
$azureResourceGroup = "<AZURE_RESOURCE_GROUP>"

# Optional: auto-detect from your current Azure CLI context.
# If you prefer explicit values, replace these with literal IDs.
$azureSubscriptionId = az account show --query id -o tsv
$azureTenantId = az account show --query tenantId -o tsv

Write-Host "ClientId: $azureClientId"
Write-Host "TenantId: $azureTenantId"
Write-Host "SubscriptionId: $azureSubscriptionId"
Write-Host "ResourceGroup: $azureResourceGroup"

# Resolve service principal object id from app/client id.
$spObjectId = az ad sp show --id $azureClientId --query id -o tsv

if (-not $spObjectId) {
  throw "Unable to resolve service principal object id for client id $azureClientId"
}

# Grant required roles on the deployment resource group.
az role assignment create `
  --assignee-object-id $spObjectId `
  --assignee-principal-type ServicePrincipal `
  --role Contributor `
  --scope "/subscriptions/$azureSubscriptionId/resourceGroups/$azureResourceGroup"

az role assignment create `
  --assignee-object-id $spObjectId `
  --assignee-principal-type ServicePrincipal `
  --role "User Access Administrator" `
  --scope "/subscriptions/$azureSubscriptionId/resourceGroups/$azureResourceGroup"

# Verify assignment.
az role assignment list `
  --assignee $azureClientId `
  --scope "/subscriptions/$azureSubscriptionId/resourceGroups/$azureResourceGroup" `
  --query "[].{role:roleDefinitionName,scope:scope}" `
  --output table
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
- `az deployment group create` fails on `Microsoft.Authorization/roleAssignments`:
  - Verify the deployment principal has both `Contributor` and `User Access Administrator` on the target resource group.
- Authorization failures during `az deployment group create`:
  - Verify the required role assignments exist on the target resource group.
- Missing workflow configuration errors:
  - Ensure all required GitHub variables and secrets are present in environment `dev`.
- App deployment succeeds but site behavior is wrong:
  - Validate App Service settings include `ASPNETCORE_ENVIRONMENT=Production` and expected `APP_ENVIRONMENT`.
