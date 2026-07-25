namespace InterviewSimulator.Api.Features.Identity.CurrentUser;

public interface IUserProfileStore
{
    Task UpsertAuthenticatedUserProfileAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default);
}