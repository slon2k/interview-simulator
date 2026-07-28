using System.Net;

using InterviewSimulator.Api.Features.Interviews;

using Microsoft.Azure.Cosmos;

namespace InterviewSimulator.Api.Infrastructure.Interviews;

public sealed class CosmosInterviewStore(Container container) : IInterviewStore
{

    public async Task CreateInterviewAsync(
        InterviewSession session,
        InterviewTurn firstTurn,
        CancellationToken cancellationToken = default)
    {
        if (firstTurn.SessionId != session.Id)
        {
            throw new InvalidOperationException("First turn must belong to the session.");
        }

        if (firstTurn.UserId != session.UserId)
        {
            throw new InvalidOperationException("First turn user id must match session user id.");
        }

        if (firstTurn.TurnNumber != 1)
        {
            throw new InvalidOperationException("First turn number must be 1.");
        }

        if (firstTurn.IsAnswered)
        {
            throw new InvalidOperationException("First turn must not be answered.");
        }

        var sessionDocument = CosmosSessionDocument.FromDomain(session);
        var firstTurnDocument = CosmosTurnDocument.FromDomain(firstTurn);

        var partitionKey = new PartitionKey(session.UserId);

        using var response = await container
            .CreateTransactionalBatch(partitionKey)
            .CreateItem(sessionDocument)
            .CreateItem(firstTurnDocument)
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

    public Task<IReadOnlyList<InterviewTurn>> ListTurnsAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task SaveAnswerSubmissionAsync(
        InterviewSession session,
        InterviewTurn answeredTurn,
        InterviewTurn? nextTurn,
        CancellationToken cancellationToken = default)
    {
        if (answeredTurn.SessionId != session.Id)
        {
            throw new InvalidOperationException("Answered turn must belong to the session.");
        }

        if (answeredTurn.UserId != session.UserId)
        {
            throw new InvalidOperationException("Answered turn user id must match session user id.");
        }

        if (!answeredTurn.IsAnswered)
        {
            throw new InvalidOperationException("Answered turn must have an answer.");
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

            if (nextTurn.TurnNumber != answeredTurn.TurnNumber + 1)
            {
                throw new InvalidOperationException("Next turn number must follow answered turn number.");
            }

            if (nextTurn.IsAnswered)
            {
                throw new InvalidOperationException("Next turn must not be answered.");
            }
        }

        var sessionDocument = CosmosSessionDocument.FromDomain(session);
        var answeredTurnDocument = CosmosTurnDocument.FromDomain(answeredTurn);

        var partitionKey = new PartitionKey(session.UserId);

        var batch = container
            .CreateTransactionalBatch(partitionKey)
            .ReplaceItem(answeredTurnDocument.Id, answeredTurnDocument)
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

    public async Task SaveSessionAsync(
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