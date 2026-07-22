namespace InterviewSimulator.Api.Features.Auth;

public sealed class AccessControlOptions
{
    public const string SectionName = "AccessControl";

    /// <summary>
    /// Canonical application user IDs allowed to use the app.
    /// Example: github|12345678
    /// </summary>
    public string[] InvitedUserIds { get; init; } = [];

    /// <summary>
    /// Canonical application user IDs with admin access.
    /// Admins are treated as invited users by access-control logic.
    /// Example: github|12345678
    /// </summary>
    public string[] AdminUserIds { get; init; } = [];
}