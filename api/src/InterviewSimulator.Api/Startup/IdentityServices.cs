using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.Authorization;
using InterviewSimulator.Api.Features.Identity.CurrentUser;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Startup;

/// <summary>
/// Registers identity and authorization application services.
/// These are orchestration services that coordinate access control, authorization, and user profile management.
/// </summary>
public static class IdentityServices
{
    public static WebApplicationBuilder AddIdentityServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AccessControlOptions>()
            .Bind(builder.Configuration.GetSection(AccessControlOptions.SectionName))
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<AccessControlOptions>, AccessControlOptionsValidator>();

        builder.Services.AddScoped<IAccessControlService, AccessControlService>();
        builder.Services.AddScoped<IAuthorizationHandler, InvitedUserAuthorizationHandler>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        return builder;
    }
}
