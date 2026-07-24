using InterviewSimulator.Api.Infrastructure.Cosmos;

using Microsoft.Extensions.Options;
using Microsoft.Azure.Cosmos;
using Azure.Identity;
using InterviewSimulator.Api.Infrastructure.Data;

namespace InterviewSimulator.Api.Startup;

public static class CosmosPersistence
{
    public static WebApplicationBuilder AddCosmosPersistence(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services
            .AddOptions<CosmosDbOptions>()
            .Bind(configuration.GetSection(CosmosDbOptions.SectionName))
            .Validate(ValidateCosmosOptions, BuildValidationFailureMessage())
            .ValidateOnStart();

        var cosmosOptions = configuration
            .GetSection(CosmosDbOptions.SectionName)
            .Get<CosmosDbOptions>() ?? new CosmosDbOptions();

        if (!cosmosOptions.Enabled)
        {
            return builder;
        }

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

            var clientOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return new CosmosClient(options.ConnectionString, clientOptions);
            }

            return new CosmosClient(
                accountEndpoint: options.Endpoint,
                tokenCredential: new DefaultAzureCredential(),
                clientOptions: clientOptions);
        });

        services.AddScoped<ICosmosDbInitializer, CosmosDbInitializer>();

        return builder;
    }

    private static bool ValidateCosmosOptions(CosmosDbOptions options)
    {
        if (!options.Enabled)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.Endpoint)
            && !string.IsNullOrWhiteSpace(options.DatabaseName)
            && !string.IsNullOrWhiteSpace(options.UsersContainerName)
            && !string.IsNullOrWhiteSpace(options.SessionsContainerName);
    }

    private static string BuildValidationFailureMessage()
    {
        return string.Join(
            " ",
            "Cosmos DB persistence is enabled, but required configuration is missing.",
            "Required settings:",
            "CosmosDb:Endpoint,",
            "CosmosDb:DatabaseName,",
            "CosmosDb:UsersContainerName,",
            "CosmosDb:SessionsContainerName.");
    }
}