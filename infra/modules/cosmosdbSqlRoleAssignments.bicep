@description('Name of the existing Cosmos DB account.')
param cosmosAccountName string

@description('Principal IDs to grant Cosmos DB SQL Built-in Data Contributor.')
param principalIds array = []

var cosmosDbSqlDataContributorRoleDefinitionId = '00000000-0000-0000-0000-000000000002'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
}

resource sqlDataContributorAssignments 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = [for principalId in principalIds: {
  parent: cosmosAccount
  name: guid(cosmosAccount.id, principalId, cosmosDbSqlDataContributorRoleDefinitionId)
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/${cosmosDbSqlDataContributorRoleDefinitionId}'
    principalId: principalId
    scope: cosmosAccount.id
  }
}]
