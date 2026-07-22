using InterviewSimulator.Api.Features.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewSimulator.Api.Startup;

public static class Services
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IAccessControlService, ConfigAccessControlService>();
        return builder;
    }
}