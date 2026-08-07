using System.Security.Claims;

namespace InterviewSimulator.Api.Features.Identity.CurrentUser;

public static class CurrentUserEndpoint
{
    public static IEndpointRouteBuilder MapCurrentUser(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", CurrentUserHandler)
            .WithName("GetCurrentUser")
            .WithSummary("Get current user information")
            .WithDescription("Returns information about the currently authenticated user, including their authentication status, invitation status, admin status, and profile details.")
            .Produces<CurrentUserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> CurrentUserHandler(
        ClaimsPrincipal user,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await currentUserService.GetCurrentUserAsync(user, cancellationToken);

        return Results.Ok(new CurrentUserResponse(
            IsAuthenticated: currentUser.IsAuthenticated,
            IsInvited: currentUser.IsInvited,
            IsAdmin: currentUser.IsAdmin,
            UserId: currentUser.UserId,
            IdentityProvider: currentUser.IdentityProvider,
            DisplayName: currentUser.DisplayName,
            GithubLogin: currentUser.GithubLogin,
            AvatarUrl: currentUser.AvatarUrl));
    }
}

public sealed record CurrentUserResponse(
    bool IsAuthenticated,
    bool IsInvited,
    bool IsAdmin,
    string? UserId,
    string? IdentityProvider,
    string? DisplayName,
    string? GithubLogin,
    string? AvatarUrl);