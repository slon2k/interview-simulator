@description('Name of the Azure OpenAI account. Must be globally unique.')
param accountName string

@description('Environment moniker, e.g. "dev", "test", "prod".')
param environment string

@description('Azure region for the OpenAI resource.')
param location string

@description('Azure OpenAI SKU.')
@allowed([
  'S0'
])
param skuName string = 'S0'

@description('Whether to create an Azure OpenAI model deployment in this module.')
param deployModel bool = false

@description('List of model deployments. When deployModel=true, each object should contain name, modelName, modelVersion, deploymentSkuName, and deploymentCapacity.')
param deployments array = []

@description('Extra tags merged into the defaults.')
param extraTags object = {}

@description('Principal IDs (object IDs) to grant the Cognitive Services OpenAI User role. Typically includes the App Service managed identity.')
param openAIUserPrincipalIds array = []

var cognitiveServicesOpenAIUserRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd' // Cognitive Services OpenAI User
)

var normalizedEnvironment = toLower(environment)

var baseTags = {
  environment: normalizedEnvironment
  workload: 'openai'
  managedBy: 'iac'
}

var finalTags = union(baseTags, extraTags)

var effectiveDeployments = deployModel
  ? deployments
  : []

var effectiveDeploymentName = empty(effectiveDeployments) ? '' : string(first(effectiveDeployments).name)

resource openAIAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: accountName
  location: location
  kind: 'OpenAI'
  sku: {
    name: skuName
  }
  tags: finalTags
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
}

resource openAIModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = [for deployment in effectiveDeployments: {
  parent: openAIAccount
  name: string(deployment.name)
  sku: {
    name: string(deployment.deploymentSkuName)
    capacity: int(deployment.deploymentCapacity)
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: string(deployment.modelName)
      version: string(deployment.modelVersion)
    }
  }
}]

resource openAIUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in openAIUserPrincipalIds: {
  scope: openAIAccount
  name: guid(openAIAccount.id, principalId, cognitiveServicesOpenAIUserRoleId)
  properties: {
    roleDefinitionId: cognitiveServicesOpenAIUserRoleId
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}]

output openAIAccountName string = openAIAccount.name
output openAIAccountId string = openAIAccount.id
output openAIEndpoint string = openAIAccount.properties.endpoint
output openAIDeploymentName string = effectiveDeploymentName
output openAIDeploymentNames array = [for deployment in effectiveDeployments: string(deployment.name)]
