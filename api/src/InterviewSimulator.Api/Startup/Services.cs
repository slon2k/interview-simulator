using InterviewSimulator.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;

namespace InterviewSimulator.Api.Startup;

public static class Services
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IAccessControlService, ConfigAccessControlService>();
        builder.Services.AddSingleton<IAuthorizationHandler, InvitedUserAuthorizationHandler>();

        return builder;
    }
}