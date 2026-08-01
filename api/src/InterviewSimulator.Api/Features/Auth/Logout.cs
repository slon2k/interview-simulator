using Microsoft.AspNetCore.Authentication.Cookies;

namespace InterviewSimulator.Api.Features.Auth;

public static class Logout
{
    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", Handler)
            .RequireAuthorization();

        return app;
    }

    private static IResult Handler()
    {
        return Results.SignOut(
            authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme]);
    }
}

