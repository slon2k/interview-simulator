@description('Base workload name, e.g. "interview".')
@maxLength(40)
param baseName string

@description('Environment moniker: "dev", "test", or "prod".')
@allowed(['dev', 'test', 'prod'])
param environment string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('App Service plan SKU name.')
@allowed(['F1', 'B1', 'B2', 'B3', 'S1', 'S2', 'S3', 'P0v3', 'P1v3', 'P2v3', 'P3v3'])
param skuName string = 'B1'

@description('Number of workers for the App Service plan.')
@minValue(1)
param skuCapacity int = 1

@description('Linux runtime stack in <RUNTIME>|<VERSION> format.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

@description('Extra tags merged into the resource defaults.')
param extraTags object = {}

@description('Enable Key Vault purge protection. Irreversible once set. Set true for prod to prevent permanent secret loss.')
param enablePurgeProtection bool = false

@description('Name of the Key Vault.')
// Simpler, unique, and more readable Key Vault name: baseName-env-kv-xxxxxx
param keyVaultName string = toLower('${take(baseName, 8)}-${environment}-kv-${take(uniqueString(resourceGroup().id), 6)}')

// speech account name must be globally unique, so we use a deterministic suffix to avoid collisions
@description('Speech account name, should be globally unique.')
param speechAccountName string = toLower('${take(baseName, 16)}-${environment}-spch-${take(uniqueString(resourceGroup().id), 6)}')

@description('Azure Speech SKU for dev/test/prod.')
@allowed([
  'F0'
  'S0'
])
param speechSkuName string = 'F0'

var appNameSuffix = take(uniqueString(subscription().id, resourceGroup().id, baseName, environment), 6)


// ── Modules ──────────────────────────────────────────────────────────────────

module plan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    baseName: baseName
    environment: environment
    location: location
    skuName: skuName
    skuCapacity: skuCapacity
    extraTags: extraTags
  }
}

module api 'modules/appService.bicep' = {
  name: 'appService'
  params: {
    baseName: baseName
    environment: environment
    appNameSuffix: appNameSuffix
    location: location
    appServicePlanId: plan.outputs.appServicePlanId
    linuxFxVersion: linuxFxVersion
    alwaysOn: skuName != 'F1'

    applicationInsightsConnectionString: appInsights.outputs.connectionString
    appSettings: [
      {
        name: 'AzureSpeech__Region'
        value: speech.outputs.speechRegion
      }
      {
        name: 'AzureSpeech__Endpoint'
        value: speech.outputs.speechEndpoint
      }
      {
        name: 'AzureSpeech__TokenEndpoint'
        value: speech.outputs.speechTokenEndpoint
      }
      {
        name: 'AzureSpeech__Key'
        value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=azure-speech-key)'
      }
    ]
    extraTags: extraTags
  }
}


module appInsights 'modules/applicationInsights.bicep' = {
  name: 'applicationInsights'
  params: {
    baseName: baseName
    environment: environment
    location: location
    extraTags: extraTags
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    keyVaultName: keyVaultName
    location: location
    enablePurgeProtection: enablePurgeProtection
    secretsUserPrincipalIds: [api.outputs.webAppPrincipalId]
  }
}

module speech 'modules/speechService.bicep' = {
  name: 'speechService'
  params: {
    speechAccountName: speechAccountName
    environment: environment
    location: location
    skuName: speechSkuName
    extraTags: extraTags
  }
}

// ── Variables for Outputs ─────────────────────────────────────────────────────

output appServicePlanName string = plan.outputs.appServicePlanName
output appServicePlanId string = plan.outputs.appServicePlanId
output webAppName string = api.outputs.webAppName
output webAppId string = api.outputs.webAppId
output webAppDefaultHostName string = api.outputs.webAppDefaultHostName
output webAppUrl string = 'https://${api.outputs.webAppDefaultHostName}'
output webAppPrincipalId string = api.outputs.webAppPrincipalId
output appInsightsName string = appInsights.outputs.appInsightsName
output appInsightsId string = appInsights.outputs.appInsightsId
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
output speechAccountName string = speech.outputs.speechAccountName
output speechAccountId string = speech.outputs.speechAccountId
output speechRegion string = speech.outputs.speechRegion
output speechEndpoint string = speech.outputs.speechEndpoint
output speechTokenEndpoint string = speech.outputs.speechTokenEndpoint
