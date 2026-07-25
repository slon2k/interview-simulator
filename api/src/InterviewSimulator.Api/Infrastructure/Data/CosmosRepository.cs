using Microsoft.Azure.Cosmos;

namespace InterviewSimulator.Api.Infrastructure.Data;

public sealed class CosmosRepository<TDocument>(Container container) : IRepository<TDocument>
    where TDocument : IDocument
{
    public async Task<TDocument?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await container.ReadItemAsync<TDocument>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    public async Task<TDocument> UpsertAsync(TDocument document, string partitionKey, CancellationToken cancellationToken = default)
    {
        var response = await container.UpsertItemAsync(document, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        await container.DeleteItemAsync<TDocument>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
    }
}