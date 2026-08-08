using Azure.Identity;

using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.Profile;
using InterviewSimulator.Api.Features.Interviews;
using InterviewSimulator.Api.Infrastructure.Cosmos;
using InterviewSimulator.Api.Infrastructure.Data;
using InterviewSimulator.Api.Infrastructure.Identity;
using InterviewSimulator.Api.Infrastructure.Interviews;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Startup;

public static class PersistenceServices
{
    public static WebApplicationBuilder AddPersistenceServices(this WebApplicationBuilder builder)
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
            services.AddScoped<DisabledIdentityUserStore>();

            services.AddScoped<IUserProfileStore>(
                sp => sp.GetRequiredService<DisabledIdentityUserStore>());

            services.AddScoped<IUserAccessReader>(
                sp => sp.GetRequiredService<DisabledIdentityUserStore>());

            services.AddScoped<IInterviewStore, DisabledInterviewStore>();

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

        services.AddScoped<IInterviewStore>(sp => new CosmosInterviewStore(sp.GetRequiredService<CosmosClient>()
            .GetContainer(
                cosmosOptions.DatabaseName,
                cosmosOptions.SessionsContainerName)));

        services.AddScoped(sp => new CosmosIdentityUserStore(
            sp.GetRequiredService<CosmosClient>()
                .GetContainer(
                    cosmosOptions.DatabaseName,
                    cosmosOptions.UsersContainerName)));

        services.AddScoped<IUserProfileStore>(sp => sp.GetRequiredService<CosmosIdentityUserStore>());
        services.AddScoped<IUserAccessReader>(sp => sp.GetRequiredService<CosmosIdentityUserStore>());

        return builder;
    }

    public static async Task InitializeCosmosPersistenceAsync(this WebApplication app)
    {
        var options = app.Services
            .GetRequiredService<IOptions<CosmosDbOptions>>()
            .Value;

        if (!options.Enabled || !options.InitializeOnStartup)
        {
            return;
        }

        using var scope = app.Services.CreateScope();

        var initializer = scope.ServiceProvider.GetRequiredService<ICosmosDbInitializer>();

        await initializer.InitializeAsync(app.Lifetime.ApplicationStopping);
    }

    private static bool ValidateCosmosOptions(CosmosDbOptions options)
    {
        if (!options.Enabled)
        {
            return true;
        }

        var hasEndpoint = !string.IsNullOrWhiteSpace(options.Endpoint);
        var hasConnectionString = !string.IsNullOrWhiteSpace(options.ConnectionString);

        return (hasEndpoint || hasConnectionString)
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
            "CosmosDb:DatabaseName,",
            "CosmosDb:UsersContainerName,",
            "CosmosDb:SessionsContainerName,",
            "and either CosmosDb:Endpoint for managed identity or CosmosDb:ConnectionString for local emulator/key auth.");
    }
}