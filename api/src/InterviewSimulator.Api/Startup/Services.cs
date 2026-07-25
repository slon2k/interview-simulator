using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.Authorization;
using InterviewSimulator.Api.Features.Identity.CurrentUser;

using Microsoft.AspNetCore.Authorization;

namespace InterviewSimulator.Api.Startup;

public static class Services
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAccessControlService, AccessControlService>();
        builder.Services.AddScoped<IAuthorizationHandler, InvitedUserAuthorizationHandler>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        return builder;
    }
}