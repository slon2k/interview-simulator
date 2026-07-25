namespace InterviewSimulator.Api.Features.Identity.Access;

public interface IUserAccessReader
{
    Task<UserAccessSnapshot?> GetAccessByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed record UserAccessSnapshot(
    string UserId,
    string AccessLevel,
    bool IsDisabled);