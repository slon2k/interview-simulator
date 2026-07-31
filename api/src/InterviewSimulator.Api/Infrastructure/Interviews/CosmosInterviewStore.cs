using System.Globalization;
using System.Net;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Infrastructure.Data;

using Microsoft.Azure.Cosmos;

namespace InterviewSimulator.Api.Infrastructure.Interviews;

public sealed class CosmosInterviewStore(Container container) : IInterviewStore
{
    public async Task CreateSessionAsync(
        InterviewSession session,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionDocument = CosmosSessionDocument.FromDomain(session);
            var partitionKey = new PartitionKey(session.UserId);

            var response = await container.CreateItemAsync(
                sessionDocument,
                partitionKey,
                cancellationToken: cancellationToken);

            CosmosFailureTranslator.ThrowIfFailure(response.StatusCode, "Interviews", "CreateSession");
        }
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(ex, "Interviews", "CreateSession");
        }
    }

    public async Task StartInterviewAsync(
        InterviewSession session,
        InterviewTurn firstTurn,
        CancellationToken cancellationToken = default)
    {
        if (firstTurn.SessionId != session.Id)
        {
            throw new InvalidOperationException("Turn must belong to the session.");
        }

        if (firstTurn.UserId != session.UserId)
        {
            throw new InvalidOperationException("Turn user id must match session user id.");
        }

        try
        {
            var sessionDocument = CosmosSessionDocument.FromDomain(session);
            var turnDocument = CosmosTurnDocument.FromDomain(firstTurn);
            var sessionEtag = GetRequiredConcurrencyToken(session.ConcurrencyToken, nameof(session));

            var partitionKey = new PartitionKey(session.UserId);

            using var response = await container
                .CreateTransactionalBatch(partitionKey)
                .ReplaceItem(
                    sessionDocument.Id,
                    sessionDocument,
                    new TransactionalBatchItemRequestOptions
                    {
                        IfMatchEtag = sessionEtag
                    })
                .CreateItem(turnDocument)
                .ExecuteAsync(cancellationToken);

            CosmosFailureTranslator.ThrowIfFailure(
                response,
                "Interviews",
                "StartInterview",
                treatNotFoundAsConflict: true);
        }
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(
                ex,
                "Interviews",
                "StartInterview",
                treatNotFoundAsConflict: true);
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
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(ex, "Interviews", "GetSession");
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
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(ex, "Interviews", "GetTurn");
        }
    }

    public async Task<IReadOnlyList<InterviewSession>> ListSessionsAsync(
        string userId,
        IReadOnlyList<InterviewStatus>? statuses,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var sql = statuses is null || statuses.Count == 0
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
            AND ARRAY_CONTAINS(@statuses, c.status)
            ORDER BY c.updatedAt DESC
            """;

        var query = new QueryDefinition(sql)
            .WithParameter("@type", "session");

        if (statuses is not null && statuses.Count > 0)
        {
            query = query.WithParameter("@statuses", statuses.Select(s => s.ToString()).ToArray());
        }

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
            MaxItemCount = limit
        };

        try
        {
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
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(ex, "Interviews", "ListSessions");
        }
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

        try
        {
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
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(ex, "Interviews", "ListTurns");
        }
    }

    public async Task SaveAnswerAsync(
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
        }

        try
        {
            var sessionDocument = CosmosSessionDocument.FromDomain(session);
            var currentTurnDocument = CosmosTurnDocument.FromDomain(answeredTurn);
            var sessionEtag = GetRequiredConcurrencyToken(session.ConcurrencyToken, nameof(session));
            var answeredTurnEtag = GetRequiredConcurrencyToken(answeredTurn.ConcurrencyToken, nameof(answeredTurn));

            var partitionKey = new PartitionKey(session.UserId);

            var batch = container
                .CreateTransactionalBatch(partitionKey)
                .ReplaceItem(
                    currentTurnDocument.Id,
                    currentTurnDocument,
                    new TransactionalBatchItemRequestOptions
                    {
                        IfMatchEtag = answeredTurnEtag
                    })
                .ReplaceItem(
                    sessionDocument.Id,
                    sessionDocument,
                    new TransactionalBatchItemRequestOptions
                    {
                        IfMatchEtag = sessionEtag
                    });

            if (nextTurn is not null)
            {
                var nextTurnDocument = CosmosTurnDocument.FromDomain(nextTurn);
                batch = batch.CreateItem(nextTurnDocument);
            }

            using var response = await batch.ExecuteAsync(cancellationToken);

            CosmosFailureTranslator.ThrowIfFailure(
                response,
                "Interviews",
                "SaveAnswer",
                treatNotFoundAsConflict: true);
        }
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(
                ex,
                "Interviews",
                "SaveAnswer",
                treatNotFoundAsConflict: true);
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
                requestOptions: new ItemRequestOptions
                {
                    IfMatchEtag = GetRequiredConcurrencyToken(session.ConcurrencyToken, nameof(session))
                },
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw CosmosFailureTranslator.ToException(
                ex,
                "Interviews",
                "UpdateSession",
                treatNotFoundAsConflict: true);
        }
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(ex, "Interviews", "UpdateSession");
        }
    }

    private static string GetRequiredConcurrencyToken(string? concurrencyToken, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(concurrencyToken))
        {
            throw new InfrastructureConflictException(
                Error.Concurrency(
                    "Infrastructure.Cosmos.Interviews.MissingConcurrencyToken",
                    $"Interview persistence requires a concurrency token for {parameterName}."));
        }

        return concurrencyToken;
    }
}