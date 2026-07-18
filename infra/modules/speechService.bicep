@description('Name of the Services account for Azure Speech Services. Must be globally unique.')
param speechAccountName string

@description('Environment moniker, e.g. "dev", "test", "prod".')
param environment string

@description('Azure region for the Speech resource.')
param location string

@description('Azure Speech SKU. Use F0 for dev if available; otherwise use S0.')
@allowed([
  'F0'
  'S0'
])
param skuName string = 'F0'

@description('Whether public network access is enabled. Keep Enabled for dev.')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'

@description('Disable local/key authentication. Must be false if using Speech keys.')
param disableLocalAuth bool = false

@description('Extra tags merged into the defaults.')
param extraTags object = {}

var normalizedEnvironment = toLower(environment)

var baseTags = {
  environment: normalizedEnvironment
  workload: 'speech'
  managedBy: 'iac'
}

var finalTags = union(baseTags, extraTags)

resource speechAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: speechAccountName
  location: location
  kind: 'SpeechServices'
  sku: {
    name: skuName
  }
  tags: finalTags
  properties: {
    customSubDomainName: speechAccountName
    publicNetworkAccess: publicNetworkAccess
    disableLocalAuth: disableLocalAuth
  }
}

output speechAccountName string = speechAccount.name
output speechAccountId string = speechAccount.id
output speechRegion string = location
output speechEndpoint string = 'https://${speechAccountName}.cognitiveservices.azure.com/'
output speechTokenEndpoint string = 'https://${location}.api.cognitive.microsoft.com/sts/v1.0/issueToken'
