namespace InterviewSimulator.Api.Features.Identity.Profile;

public interface IUserProfileStore
{
    Task UpsertAuthenticatedUserProfileAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default);
}