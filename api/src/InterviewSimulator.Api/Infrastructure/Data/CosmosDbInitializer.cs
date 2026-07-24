using InterviewSimulator.Api.Infrastructure.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Infrastructure.Data;

public interface ICosmosDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class CosmosDbInitializer(
    CosmosClient cosmosClient,
    IOptions<CosmosDbOptions> options,
    ILogger<CosmosDbInitializer> logger) : ICosmosDbInitializer
{
    private readonly CosmosClient _cosmosClient = cosmosClient;
    private readonly CosmosDbOptions _options = options.Value;
    private readonly ILogger<CosmosDbInitializer> _logger = logger;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_options.InitializeOnStartup)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Initializing Cosmos DB database and containers");

            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _options.DatabaseName,
                cancellationToken: cancellationToken);

            var database = databaseResponse.Database;

            _logger.LogInformation("Database '{DatabaseName}' is ready", _options.DatabaseName);

            await CreateContainerIfNotExistsAsync(
                database,
                _options.UsersContainerName,
                "/userId",
                cancellationToken);

            await CreateContainerIfNotExistsAsync(
                database,
                _options.SessionsContainerName,
                "/userId",
                cancellationToken);

            _logger.LogInformation("Cosmos DB initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Cosmos DB");
            throw;
        }
    }

    private async Task CreateContainerIfNotExistsAsync(
        Database database,
        string containerName,
        string partitionKeyPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = containerName,
                    PartitionKeyPath = partitionKeyPath
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Container '{ContainerName}' with partition key '{PartitionKey}' is ready",
                containerName,
                partitionKeyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create container '{ContainerName}'", containerName);
            throw;
        }
    }
}
