namespace InterviewSimulator.Api.Features.Identity.Access;

public sealed class AccessControlOptions
{
    public const string SectionName = "AccessControl";

    /// <summary>
    /// Canonical application user IDs with admin access.
    /// Admins are treated as invited users by access-control logic.
    /// Example: github|12345678
    /// </summary>
    public string[] AdminUserIds { get; init; } = [];
}