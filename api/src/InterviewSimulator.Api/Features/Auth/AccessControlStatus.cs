namespace InterviewSimulator.Api.Features.Auth;

public sealed record AccessControlStatus(
    bool IsAuthenticated,
    string? UserId,
    bool IsInvited,
    bool IsAdmin);
