using InterviewSimulator.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;

namespace InterviewSimulator.Api.Startup;

public static class Services
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAccessControlService, AccessControlService>();
        builder.Services.AddScoped<IAuthorizationHandler, InvitedUserAuthorizationHandler>();

        return builder;
    }
}