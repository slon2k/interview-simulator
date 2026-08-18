using System.Text.Json.Serialization;

using FluentValidation;
using Microsoft.Extensions.Options;

using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Features.Interviews.Ai;

namespace InterviewSimulator.Api.Startup;

public static class InterviewServices
{
    public static WebApplicationBuilder AddInterviewServices(this WebApplicationBuilder builder)
    {
        var configuredAiOptions = builder.Configuration
            .GetSection(AiOptions.SectionName)
            .Get<AiOptions>() ?? new AiOptions();

        var provider = configuredAiOptions.Provider;

        if (string.Equals(provider, AiProviders.AzureOpenAI, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddScoped<IQuestionGenerator, AzureOpenAIQuestionGenerator>();
        }
        else
        {
            builder.Services.AddScoped<IQuestionGenerator, HardcodedQuestionGenerator>();
        }

        if (string.Equals(provider, AiProviders.AzureOpenAI, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddScoped<IAnswerEvaluator, AzureOpenAIAnswerEvaluator>();
        }
        else
        {
            builder.Services.AddScoped<IAnswerEvaluator, HardcodedAnswerEvaluator>();
        }

        if (string.Equals(provider, AiProviders.AzureOpenAI, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddScoped<ISessionSummarizer, AzureOpenAISessionSummarizer>();
        }
        else
        {
            builder.Services.AddScoped<ISessionSummarizer, HardcodedSessionSummarizer>();
        }

        builder.Services.AddScoped<SessionSummaryService>();

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });


        builder.Services.AddOptions<AiOptions>()
            .Bind(builder.Configuration.GetSection(AiOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<AiOptions>, AiOptionsValidator>();
        builder.Services.AddScoped(typeof(AiStructuredOutputRunner<>));

        return builder;
    }
}
