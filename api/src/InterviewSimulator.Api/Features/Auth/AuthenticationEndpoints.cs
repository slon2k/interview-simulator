using InterviewSimulator.Api.Features.Identity.CurrentUser;

namespace InterviewSimulator.Api.Features.Auth;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapLogin();
        group.MapLogout();
        group.MapSmokeTest();

        return endpoints;
    }
}

