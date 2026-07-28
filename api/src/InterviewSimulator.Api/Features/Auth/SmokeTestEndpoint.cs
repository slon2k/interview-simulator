using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity;
using InterviewSimulator.Api.Features.Identity.Authorization;

namespace InterviewSimulator.Api.Features.Auth;

public static class SmokeTestEndpoint
{
    public static IEndpointRouteBuilder MapSmokeTest(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/smoke", SmokeHandler)
            .RequireAuthorization(AuthorizationPolicies.InvitedUser);

        return endpoints;
    }

    private static IResult SmokeHandler(ClaimsPrincipal user)
    {
        var userId = IdentityClaims.GetUserId(user);

        return Results.Ok(new
        {
            status = "authenticated",
            userId
        });
    }
}

