using InterviewSimulator.Api.Features.Dashboard;
using InterviewSimulator.Api.Features.Interviews;

using Microsoft.Azure.Cosmos;

namespace InterviewSimulator.Api.Infrastructure.Dashboard;

public sealed class CosmosDashboardQueries(Container container) : IDashboardQueries
{
    public async Task<IReadOnlyCollection<DashboardSessionProjection>> GetCompletedSessionProjectionsAsync(string userId, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT 
                c.sessionId,
                c.targetRole,
                c.focusArea,
                c.interviewType,
                c.status,
                c.completedAt,
                c.result.totalScore AS aggregateScore
            FROM c
            WHERE c.type = @sessionType AND c.userId = @userId AND c.status = @completedStatus
            """;

        var queryDefinition = new QueryDefinition(sql)
            .WithParameter("@sessionType", "session")
            .WithParameter("@userId", userId)
            .WithParameter("@completedStatus", "Completed");

        var iterator = container.GetItemQueryIterator<CosmosDashboardSessionProjection>(
            queryDefinition,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userId)
            });
        var results = new List<CosmosDashboardSessionProjection>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }
        return [.. results.Select(ToDashboardSessionProjection)];
    }

    public Task<IReadOnlyCollection<DashboardDimensionProjection>> GetEvaluatedTurnDimensionProjectionsAsync(string userId, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT
                c.sessionId,
                d.key,
                d.label,
                d.score
            FROM c
            JOIN d IN c.evaluation.dimensions
            WHERE c.type = @turnType
            AND c.userId = @userId
            AND IS_DEFINED(c.evaluation)    
            """;

        var queryDefinition = new QueryDefinition(sql)
            .WithParameter("@turnType", "turn")
            .WithParameter("@userId", userId);

        var iterator = container.GetItemQueryIterator<DashboardDimensionProjection>(
            queryDefinition,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userId)
            });
        var results = new List<DashboardDimensionProjection>();
        return ReadAllAsync(iterator, results, cancellationToken);
    }

    private static async Task<IReadOnlyCollection<T>> ReadAllAsync<T>(FeedIterator<T> iterator, List<T> results, CancellationToken cancellationToken)
    {
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }
        return results;
    }

    private sealed record CosmosDashboardSessionProjection(
        string SessionId,
        string TargetRole,
        string FocusArea,
        string InterviewType,
        string Status,
        DateTime? CompletedAt,
        int? AggregateScore
    );

    private static DashboardSessionProjection ToDashboardSessionProjection(CosmosDashboardSessionProjection cosmosProjection) =>
        new(
            Guid.Parse(cosmosProjection.SessionId),
            cosmosProjection.TargetRole,
            cosmosProjection.FocusArea,
            Enum.Parse<InterviewType>(cosmosProjection.InterviewType),
            Enum.Parse<InterviewStatus>(cosmosProjection.Status),
            cosmosProjection.CompletedAt,
            cosmosProjection.AggregateScore
        );
}