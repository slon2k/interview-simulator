using System.Globalization;
using System.Net;

using InterviewSimulator.Api.Features.Interviews;

using Microsoft.Azure.Cosmos;

namespace InterviewSimulator.Api.Infrastructure.Interviews;

public sealed class CosmosInterviewStore(Container container) : IInterviewStore
{
    public async Task CreateSessionAsync(
        InterviewSession session,
        CancellationToken cancellationToken = default)
    {
        var sessionDocument = CosmosSessionDocument.FromDomain(session);
        var partitionKey = new PartitionKey(session.UserId);

        var response = await container.CreateItemAsync(
            sessionDocument,
            partitionKey,
            cancellationToken: cancellationToken);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Failed to create interview. Status code: {response.StatusCode}");
        }
    }

    public async Task CreateTurnAsync(
        InterviewSession session,
        InterviewTurn turn,
        CancellationToken cancellationToken = default)
    {
        if (turn.SessionId != session.Id)
        {
            throw new InvalidOperationException("Turn must belong to the session.");
        }

        if (turn.UserId != session.UserId)
        {
            throw new InvalidOperationException("Turn user id must match session user id.");
        }

        var sessionDocument = CosmosSessionDocument.FromDomain(session);
        var turnDocument = CosmosTurnDocument.FromDomain(turn);

        var partitionKey = new PartitionKey(session.UserId);

        using var response = await container
            .CreateTransactionalBatch(partitionKey)
            .ReplaceItem(sessionDocument.Id, sessionDocument)
            .CreateItem(turnDocument)
            .ExecuteAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to create interview. Status code: {response.StatusCode}");
        }
    }

    public async Task<InterviewSession?> GetSessionAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var id = CosmosSessionDocument.ToCosmosId(sessionId);

        try
        {
            var response = await container.ReadItemAsync<CosmosSessionDocument>(
                id,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            return response.Resource.ToDomain();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<InterviewTurn?> GetTurnAsync(
        string userId,
        Guid sessionId,
        int turnNumber,
        CancellationToken cancellationToken = default)
    {
        var id = CosmosTurnDocument.ToCosmosId(sessionId, turnNumber);

        try
        {
            var response = await container.ReadItemAsync<CosmosTurnDocument>(
                id,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            return response.Resource.ToDomain();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
        string userId,
        InterviewStatus? status,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var sql = status is null
            ? """
            SELECT *
            FROM c
            WHERE c.type = @type
            ORDER BY c.updatedAt DESC
            """
            : """
            SELECT *
            FROM c
            WHERE c.type = @type
            AND c.status = @status
            ORDER BY c.updatedAt DESC
            """;

        var query = new QueryDefinition(sql)
            .WithParameter("@type", "session");

        if (status is not null)
        {
            query = query.WithParameter("@status", status.ToString());
        }

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
            MaxItemCount = limit
        };

        using var iterator = container.GetItemQueryIterator<CosmosSessionDocument>(
            query,
            requestOptions: options);

        var sessions = new List<InterviewSession>();

        while (iterator.HasMoreResults && sessions.Count < limit)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);

            foreach (var document in page)
            {
                sessions.Add(document.ToDomain());

                if (sessions.Count >= limit)
                {
                    break;
                }
            }
        }

        return sessions;
    }

    public async Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition(
            """
            SELECT *
            FROM c
            WHERE c.type = @type
            AND c.sessionId = @sessionId
            ORDER BY c.turnNumber ASC
            """)
            .WithParameter("@type", "turn")
            .WithParameter("@sessionId", sessionId.ToString("D", CultureInfo.InvariantCulture));

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId)
        };

        using var iterator = container.GetItemQueryIterator<CosmosTurnDocument>(
            query,
            requestOptions: options);

        var turns = new List<InterviewTurn>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            turns.AddRange(page.Select(document => document.ToDomain()));
        }

        return turns;
    }

    public async Task UpdateTurnAsync(
        InterviewSession session,
        InterviewTurn currentTurn,
        InterviewTurn? nextTurn,
        CancellationToken cancellationToken = default)
    {
        if (currentTurn.SessionId != session.Id)
        {
            throw new InvalidOperationException("Answered turn must belong to the session.");
        }

        if (currentTurn.UserId != session.UserId)
        {
            throw new InvalidOperationException("Answered turn user id must match session user id.");
        }

        if (nextTurn is not null)
        {
            if (nextTurn.SessionId != session.Id)
            {
                throw new InvalidOperationException("Next turn must belong to the session.");
            }

            if (nextTurn.UserId != session.UserId)
            {
                throw new InvalidOperationException("Next turn user id must match session user id.");
            }

            if (nextTurn.TurnNumber != currentTurn.TurnNumber + 1)
            {
                throw new InvalidOperationException("Next turn number must follow answered turn number.");
            }

            if (nextTurn.IsAnswered)
            {
                throw new InvalidOperationException("Next turn must not be answered.");
            }
        }

        var sessionDocument = CosmosSessionDocument.FromDomain(session);
        var currentTurnDocument = CosmosTurnDocument.FromDomain(currentTurn);

        var partitionKey = new PartitionKey(session.UserId);

        var batch = container
            .CreateTransactionalBatch(partitionKey)
            .ReplaceItem(currentTurnDocument.Id, currentTurnDocument)
            .ReplaceItem(sessionDocument.Id, sessionDocument);

        if (nextTurn is not null)
        {
            var nextTurnDocument = CosmosTurnDocument.FromDomain(nextTurn);
            batch = batch.CreateItem(nextTurnDocument);
        }

        using var response = await batch.ExecuteAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to save answer submission. Status code: {response.StatusCode}");
        }
    }

    public async Task UpdateSessionAsync(
        InterviewSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session, nameof(session));

        var sessionDocument = CosmosSessionDocument.FromDomain(session);
        var partitionKey = new PartitionKey(session.UserId);

        try
        {
            await container.ReplaceItemAsync(
                sessionDocument,
                sessionDocument.Id,
                partitionKey,
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Interview session with ID {session.Id} not found.", ex);
        }
    }
}