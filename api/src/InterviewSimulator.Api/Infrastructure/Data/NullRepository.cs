namespace InterviewSimulator.Api.Infrastructure.Data;

/// <summary>
/// No-op repository used when persistence is disabled.
/// Returns null for reads and no-ops for writes.
/// </summary>
internal sealed class NullRepository<TDocument> : ICosmosRepository<TDocument>
    where TDocument : ICosmosDocument
{
    public Task<TDocument?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
        => Task.FromResult<TDocument?>(default);

    public Task<TDocument> UpsertAsync(TDocument document, string partitionKey, CancellationToken cancellationToken = default)
        => Task.FromResult(document);

    public Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
