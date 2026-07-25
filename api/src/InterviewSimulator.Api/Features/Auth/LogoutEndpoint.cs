using Microsoft.AspNetCore.Authentication.Cookies;

namespace InterviewSimulator.Api.Features.Auth;

public static class LogoutEndpoint
{
    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", LogoutHandler)
            .RequireAuthorization();

        return app;
    }

    private static IResult LogoutHandler()
    {
        return Results.SignOut(
            authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme]);
    }
}

