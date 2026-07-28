namespace InterviewSimulator.Api.Infrastructure.Data;

public interface ICosmosRepository<TDocument>
    where TDocument : ICosmosDocument
{
    Task<TDocument?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    Task<TDocument> UpsertAsync(TDocument document, string partitionKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
}