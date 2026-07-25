using InterviewSimulator.Api.Features.Identity.CurrentUser;

namespace InterviewSimulator.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCurrentUser();

        return endpoints;
    }
}