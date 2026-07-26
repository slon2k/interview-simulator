namespace InterviewSimulator.Api.Infrastructure.Data;

public interface ICosmosDocument
{
    string Id { get; init; }
}

public interface IUserCosmosDocument : ICosmosDocument
{
    string UserId { get; init; }
}