using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;

using InterviewSimulator.Api.Options;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Startup;

public static class OpenAIServices
{
    /// <summary>
    /// Registers Azure OpenAI client with support for both managed identity and API key authentication.
    /// 
    /// Priority:
    /// 1. If ApiKey is configured in options, uses API key authentication
    /// 2. Otherwise, uses DefaultAzureCredential (managed identity)
    /// </summary>
    public static WebApplicationBuilder AddOpenAIServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AzureOpenAIOptions>()
            .Bind(builder.Configuration.GetSection(AzureOpenAIOptions.SectionName))
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<AzureOpenAIOptions>, AzureOpenAIOptionsValidator>();

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;

            var endpoint = new Uri(options.Endpoint);

            // Use API key if provided, otherwise use managed identity
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                return new AzureOpenAIClient(
                    endpoint,
                    new AzureKeyCredential(options.ApiKey));
            }

            // Fall back to managed identity (DefaultAzureCredential)
            return new AzureOpenAIClient(
                endpoint,
                new DefaultAzureCredential());
        });

        return builder;
    }
}
