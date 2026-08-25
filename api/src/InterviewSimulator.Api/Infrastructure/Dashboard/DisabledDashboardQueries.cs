using InterviewSimulator.Api.Features.Dashboard;

namespace InterviewSimulator.Api.Infrastructure.Dashboard;

public sealed class DisabledDashboardQueries : IDashboardQueries
{
    public Task<IReadOnlyCollection<DashboardSessionProjection>> GetCompletedSessionProjectionsAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<DashboardSessionProjection>>([]);

    public Task<IReadOnlyCollection<DashboardDimensionProjection>> GetEvaluatedTurnDimensionProjectionsAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<DashboardDimensionProjection>>([]);
}