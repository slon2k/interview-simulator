using InterviewSimulator.Api.Features.Identity;
using InterviewSimulator.Api.Features.Identity.Profile;

namespace InterviewSimulator.Api.Infrastructure.Identity;

/// <summary>
/// Middleware that persists authenticated user profiles to the user profile store.
/// 
/// This runs after authentication middleware and ensures that every authenticated user
/// is persisted (created or updated) in the persistence layer on their first request.
/// 
/// This bridges the OAuth authentication flow with the persistence layer without
/// requiring async operations in the synchronous OAuth event handlers.
/// </summary>
public sealed class UserProfilePersistenceMiddleware(RequestDelegate next, ILogger<UserProfilePersistenceMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IUserProfileStore profileStore)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                var profile = IdentityClaims.ToAuthenticatedUserProfile(context.User);
                if (profile is not null)
                {
                    await profileStore.UpsertAuthenticatedUserProfileAsync(profile, context.RequestAborted);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to persist user profile for authenticated user. Access control may treat user as guest.");
                // Don't throw — allow request to continue. Worst case: user appears as guest until next request succeeds.
            }
        }

        await next(context);
    }
}

/// <summary>
/// Extension method to add user profile persistence middleware to the application.
/// </summary>
public static class UserProfilePersistenceMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that persists authenticated user profiles.
    /// Should be called after authentication middleware and before authorization middleware.
    /// </summary>
    public static WebApplication UseUserProfilePersistence(this WebApplication app)
    {
        app.UseMiddleware<UserProfilePersistenceMiddleware>();
        return app;
    }
}
