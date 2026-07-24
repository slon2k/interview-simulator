namespace InterviewSimulator.Api.Infrastructure.Cosmos;

public sealed class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    public bool Enabled { get; init; }

    public bool InitializeOnStartup { get; init; }

    public string Endpoint { get; init; } = string.Empty;

    public string ConnectionString { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = string.Empty;

    public string UsersContainerName { get; init; } = string.Empty;

    public string SessionsContainerName { get; init; } = string.Empty;
}