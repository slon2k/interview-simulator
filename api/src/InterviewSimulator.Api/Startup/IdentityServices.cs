using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.Authorization;
using InterviewSimulator.Api.Features.Identity.CurrentUser;

using Microsoft.AspNetCore.Authorization;

namespace InterviewSimulator.Api.Startup;

/// <summary>
/// Registers identity and authorization application services.
/// These are orchestration services that coordinate access control, authorization, and user profile management.
/// </summary>
public static class IdentityServices
{
    public static WebApplicationBuilder AddIdentityServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAccessControlService, AccessControlService>();
        builder.Services.AddScoped<IAuthorizationHandler, InvitedUserAuthorizationHandler>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        return builder;
    }
}
