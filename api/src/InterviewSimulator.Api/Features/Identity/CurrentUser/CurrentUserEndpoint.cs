using System.Security.Claims;

namespace InterviewSimulator.Api.Features.Identity.CurrentUser;

public static class CurrentUserEndpoint
{
    public static IEndpointRouteBuilder MapCurrentUser(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", CurrentUserHandler)
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
            IsAuthenticated: true,
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