using Microsoft.Azure.Cosmos;

using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.Profile;
using InterviewSimulator.Api.Infrastructure.Data;

using System.Net;

namespace InterviewSimulator.Api.Infrastructure.Identity;

public sealed class CosmosIdentityUserStore(Container container) : IUserProfileStore, IUserAccessReader
{
    public async Task UpsertAuthenticatedUserProfileAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existingUser = await GetUserByIdAsync(profile.UserId, cancellationToken);
            var userDocument = UserDocumentMapper.CreateOrUpdate(existingUser, profile, DateTimeOffset.UtcNow);
            await container.UpsertItemAsync(userDocument, new PartitionKey(profile.UserId), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(ex, "UserProfiles", "UpsertProfile");
        }
    }

    private async Task<CosmosUserDocument?> GetUserByIdAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<CosmosUserDocument>(userId, new PartitionKey(userId), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (CosmosException ex)
        {
            throw CosmosFailureTranslator.ToException(ex, "UserProfiles", "GetProfile");
        }
    }

    public async Task<UserAccessSnapshot?> GetAccessByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userDocument = await GetUserByIdAsync(userId, cancellationToken);

        return userDocument is null
            ? null
            : UserDocumentMapper.ToAccessSnapshot(userDocument);
    }
}