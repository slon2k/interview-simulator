using InterviewSimulator.Api.Options;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Startup;

public static class SpeechServices
{
    public static WebApplicationBuilder AddSpeechServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AzureSpeechOptions>()
            .Bind(builder.Configuration.GetSection(AzureSpeechOptions.SectionName))
            .ValidateOnStart();
        
        builder.Services.AddSingleton<IValidateOptions<AzureSpeechOptions>, AzureSpeechOptionsValidator>();

        return builder;
    }
}