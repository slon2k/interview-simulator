using InterviewSimulator.Api.Features.Auth;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Startup;

public static class Options
{
    public static WebApplicationBuilder AddApplicationOptions(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AzureSpeechOptions>()
            .Bind(builder.Configuration.GetSection(AzureSpeechOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<AzureSpeechOptions>, AzureSpeechOptionsValidator>();

        builder.Services.AddOptions<AzureOpenAIOptions>()
            .Bind(builder.Configuration.GetSection(AzureOpenAIOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<AzureOpenAIOptions>, AzureOpenAIOptionsValidator>();

        builder.Services.AddOptions<AccessControlOptions>()
            .Bind(builder.Configuration.GetSection(AccessControlOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<AccessControlOptions>, AccessControlOptionsValidator>();

        return builder;
    }
}