namespace InterviewSimulator.Api.Features.Dashboard;

public interface IDashboardQueries
{
    Task<IReadOnlyCollection<DashboardSessionProjection>> GetCompletedSessionProjectionsAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DashboardDimensionProjection>> GetEvaluatedTurnDimensionProjectionsAsync(
        string userId,
        CancellationToken cancellationToken);
}