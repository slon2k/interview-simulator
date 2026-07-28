namespace InterviewSimulator.Api.Features.Identity.Access;

/// <summary>
/// Defines the access levels for users in the system.
/// </summary>
public static class UserAccessLevels
{
    public const string Guest = "guest";
    public const string Member = "member";
    public const string Admin = "admin";

    public static bool IsMemberOrAdmin(string? accessLevel) =>
        accessLevel == Member || accessLevel == Admin;

    public static bool IsAdmin(string? accessLevel) =>
        accessLevel == Admin;
}