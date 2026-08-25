using InterviewSimulator.Api.Features.Identity.Authorization;

namespace InterviewSimulator.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard")
            .RequireAuthorization(AuthorizationPolicies.InvitedUser)
            .WithTags("Dashboard");

        group.MapGetDashboard();

        return endpoints;
    }
}