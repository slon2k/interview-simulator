# Infrastructure

This folder contains the Bicep infrastructure baseline for Interview Simulator.

## Scope

The current infrastructure provisions:

- Linux App Service plan
- Linux App Service web app for the ASP.NET Core backend
- Application Insights
- Log Analytics workspace

The frontend is not deployed as a separate static site. The production model is a single App Service that serves the published ASP.NET Core app and the built Vite frontend.

## Layout

- `main.bicep`: resource group entrypoint template
- `dev.bicepparam`: development environment parameters
- `modules/appServicePlan.bicep`: App Service plan module
- `modules/appService.bicep`: App Service web app module
- `modules/applicationInsights.bicep`: observability module
- `main.json`: generated ARM template output from Bicep build

## Naming

Resource names follow a deterministic pattern based on:

- `baseName`
- `environment`
- resource type suffix

The App Service site name also includes a deterministic unique suffix derived from subscription, resource group, base name, and environment. This avoids global App Service DNS name collisions while keeping names stable across redeployments.

## Parameters

The main template currently accepts these parameters:

- `baseName`: base workload name
- `environment`: `dev`, `test`, or `prod`
- `location`: Azure region
- `skuName`: App Service plan SKU
- `skuCapacity`: App Service plan worker count
- `linuxFxVersion`: Linux runtime stack in `<RUNTIME>|<VERSION>` format
- `extraTags`: additional Azure tags merged into the defaults

## Current Defaults

The development parameter file in [dev.bicepparam](c:/Projects/Training/2026/interview-simulator/infra/dev.bicepparam) currently uses:

- `baseName = 'interview-sim'`
- `environment = 'dev'`
- `skuName = 'F1'`
- `skuCapacity = 1`
- `linuxFxVersion = 'DOTNETCORE|10.0'`

## Deploy

Deploy to a resource group:

```powershell
az deployment group create \
  --resource-group rg-interview-sim-dev \
  --template-file infra/main.bicep \
  --parameters infra/dev.bicepparam
```

Run a what-if preview:

```powershell
az deployment group what-if \
  --resource-group rg-interview-sim-dev \
  --template-file infra/main.bicep \
  --parameters infra/dev.bicepparam
```

## Validate

Compile the Bicep template:

```powershell
az bicep build --file infra/main.bicep
```

## Notes

- `main.json` is generated output. Do not hand-edit it.
- Keep reusable resources in `modules/` instead of growing `main.bicep` indefinitely.
- Keep secrets out of parameter files committed to the repository.
- If a new environment is introduced, add a new `*.bicepparam` file and document it here.
- App Service runtime mode is set with `ASPNETCORE_ENVIRONMENT=Production` for hosted deployments.
- Application-specific environment routing uses `APP_ENVIRONMENT` (for example `dev`, `test`, `prod`).
