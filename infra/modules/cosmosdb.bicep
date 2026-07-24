@description('Name of the Cosmos DB account.')
param cosmosAccountName string

@description('Location for Cosmos DB.')
param location string = resourceGroup().location

@description('Cosmos DB SQL API database name.')
param databaseName string

@description('Cosmos DB container configurations. Each item should contain: name and partitionKey.')
param containers array = []

@description('Cosmos DB API default consistency level.')
@allowed(['Strong', 'BoundedStaleness', 'Session', 'ConsistentPrefix', 'Eventual'])
param consistencyLevel string = 'Session'

@description('Enable automatic failover.')
param automaticFailover bool = false

@description('Enable free tier. Only available for one account per subscription.')
param enableFreeTier bool = false

@description('Enable serverless capability for Cosmos DB.')
param enableServerless bool = false

@description('Database-level shared throughput for provisioned Cosmos DB accounts. Ignored when serverless is enabled.')
@minValue(400)
param databaseThroughput int = 400

var effectiveConsistencyPolicy = union(
  {
    defaultConsistencyLevel: consistencyLevel
  },
  consistencyLevel == 'BoundedStaleness'
    ? {
        maxIntervalInSeconds: 5
        maxStalenessPrefix: 100
      }
    : {}
)

var cosmosAccountProperties = union(
  {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: enableFreeTier
    consistencyPolicy: effectiveConsistencyPolicy
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    enableAutomaticFailover: automaticFailover
  },
  enableServerless
    ? {
        capabilities: [
          {
            name: 'EnableServerless'
          }
        ]
      }
    : {}
)

var databaseProperties = union(
  {
    resource: {
      id: databaseName
    }
  },
  enableServerless
    ? {}
    : {
        options: {
          throughput: databaseThroughput
        }
      }
)

// ── Cosmos DB Account ──────────────────────────────────────────────────────

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: cosmosAccountProperties
}

// ── Cosmos DB Database ──────────────────────────────────────────────────────

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: databaseProperties
}

// ── Cosmos DB Containers ────────────────────────────────────────────────────

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = [for containerConfig in containers: {
  parent: database
  name: containerConfig.name
  properties: {
    resource: {
      id: containerConfig.name
      partitionKey: {
        paths: [
          containerConfig.partitionKey
        ]
        kind: 'Hash'
      }
    }
  }
}]

// ── Outputs ─────────────────────────────────────────────────────────────────

@description('The Cosmos DB account name.')
output accountName string = cosmosAccount.name

@description('The Cosmos DB account endpoint.')
output endpoint string = cosmosAccount.properties.documentEndpoint

@description('The Cosmos DB database name.')
output databaseName string = database.name

@description('The Cosmos DB account resource ID.')
output accountId string = cosmosAccount.id
