using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity;
using InterviewSimulator.Api.Features.Identity.Authorization;

namespace InterviewSimulator.Api.Features.Auth;

public static class SmokeTestEndpoint
{
    public static IEndpointRouteBuilder MapSmokeTest(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/smoke", Handler)
            .RequireAuthorization(AuthorizationPolicies.InvitedUser)
            .WithName("SmokeTest")
            .WithSummary("Verify authenticated access")
            .WithDescription("Returns the authenticated user's ID. Requires the InvitedUser authorization policy.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static IResult Handler(ClaimsPrincipal user)
    {
        var userId = IdentityClaims.GetUserId(user);

        return Results.Ok(new Response("authenticated", userId));
    }

    public record Response(string Status, string? UserId);
}
