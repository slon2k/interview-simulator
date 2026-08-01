using Microsoft.AspNetCore.Authentication.Cookies;

namespace InterviewSimulator.Api.Features.Auth;

public static class Logout
{
    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", Handler)
            .RequireAuthorization()
            .WithName("Logout")
            .WithSummary("Sign out the current user")
            .WithDescription("Clears the authentication cookie and signs out the authenticated user.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static IResult Handler()
    {
        return Results.SignOut(
            authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme]);
    }
}
