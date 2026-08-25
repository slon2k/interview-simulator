using System.Security.Claims;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Dashboard;

public static class GetDashboard
{
    public static IEndpointRouteBuilder MapGetDashboard(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", Handler)
        .RequireAuthorization()
        .WithName("GetDashboard")
        .WithSummary("Get dashboard metrics")
        .WithDescription("Returns dashboard metrics for the authenticated user.")
        .WithTags("Dashboard")
        .Produces<DashboardResponse>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> Handler(
        DashboardMetricsCalculator metricsCalculator,
        IDashboardQueries dashboardQueries,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Errors.Unauthorized.ToProblemResult();
        }

        var sessionProjections = await dashboardQueries.GetCompletedSessionProjectionsAsync(userId, cancellationToken);
        var dimensionProjections = await dashboardQueries.GetEvaluatedTurnDimensionProjectionsAsync(userId, cancellationToken);
        var response = metricsCalculator.Calculate(sessionProjections, dimensionProjections);
        return Results.Ok(response);
    }

    public static class Errors
    {
        public static Error Unauthorized => Error.Unauthorized("Dashboard.GetDashboard.Unauthorized", "Authentication is required.");
    }
}