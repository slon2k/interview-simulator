@description('Base workload name, e.g. "interview".')
@maxLength(40)
param baseName string

@description('Environment moniker, e.g. "dev", "test", "prod".')
param environment string

@description('Deterministic suffix used to keep App Service names globally unique.')
param appNameSuffix string

@description('Azure region for all resources.')
param location string

@description('Resource ID of the App Service plan to host this app.')
param appServicePlanId string

@description('Linux runtime stack in <RUNTIME>|<VERSION> format.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

@description('Enable AlwaysOn for the web app. Disable for Free (F1) tier plans.')
param alwaysOn bool = true

@description('Enable client affinity (ARR). Normally false for APIs.')
param clientAffinityEnabled bool = false

@description('Deploy app as a run-from-package (zip deploy). Set false if using other deployment methods.')
param runFromPackage bool = true

@description('Additional app settings as an array of {name, value} objects.')
param appSettings array = []

@description('Connection strings (each object requires name, type, value).')
param connectionStrings array = []

@description('Application Insights connection string for telemetry. Leave empty to disable.')
param applicationInsightsConnectionString string = ''

@description('Extra tags merged into the defaults.')
param extraTags object = {}

var normalizedEnvironment = toLower(environment)
var webAppName = '${baseName}-${normalizedEnvironment}-${toLower(appNameSuffix)}'

var baseTags = {
  environment: normalizedEnvironment
  workload: 'webapi'
  managedBy: 'iac'
}

var finalTags = union(baseTags, extraTags)

var aspNetCoreEnvMap = {
  dev: 'Development'
  test: 'Staging'
  prod: 'Production'
}
var aspNetCoreEnvironment = aspNetCoreEnvMap[?normalizedEnvironment] ?? normalizedEnvironment

var defaultAppSettings = concat(
  [
    {
      name: 'ASPNETCORE_ENVIRONMENT'
      value: aspNetCoreEnvironment
    }
    {
      name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
      value: 'true'
    }
    {
      name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
      value: 'false'
    }
  ],
  runFromPackage
    ? [
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    : [],
  !empty(applicationInsightsConnectionString)
    ? [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
      ]
    : []
)

var effectiveAppSettings = concat(defaultAppSettings, appSettings)

var normalizedConnectionStrings = [
  for cs in connectionStrings: {
    name: string(cs.name)
    type: any(cs.type)
    connectionString: string(cs.value ?? cs.connectionString)
  }
]

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  tags: finalTags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    clientAffinityEnabled: clientAffinityEnabled
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: alwaysOn
      ftpsState: 'Disabled'
      http20Enabled: true
      appSettings: effectiveAppSettings
      connectionStrings: normalizedConnectionStrings
      healthCheckPath: '/api/health'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
    }
  }
}

resource webAppLogs 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: webApp
  name: 'logs'
  properties: {
    applicationLogs: {
      fileSystem: {
        level: 'Warning'
      }
    }
    httpLogs: {
      fileSystem: {
        retentionInDays: 7
        retentionInMb: 35
        enabled: true
      }
    }
  }
}

output webAppName string = webApp.name
output webAppId string = webApp.id
output webAppDefaultHostName string = webApp.properties.defaultHostName
output webAppPrincipalId string = webApp.identity.principalId
output webAppTenantId string = webApp.identity.tenantId
