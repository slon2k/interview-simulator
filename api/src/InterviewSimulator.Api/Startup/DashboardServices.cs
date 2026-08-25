using InterviewSimulator.Api.Features.Dashboard;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Startup;

public static class DashboardServices
{
    public static WebApplicationBuilder AddDashboardServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DashboardMetricsCalculator>();

        builder.Services.AddOptions<DashboardOptions>()
            .Bind(builder.Configuration.GetSection(DashboardOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<DashboardOptions>, DashboardOptionsValidator>();

        return builder;
    }
}