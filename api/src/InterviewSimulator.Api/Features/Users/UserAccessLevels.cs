namespace InterviewSimulator.Api.Features.Users;

/// <summary>
/// Defines the access levels for users in the system.
/// </summary>
public static class UserAccessLevels
{
    public const string Guest = "guest";
    public const string Member = "member";
    public const string Admin = "admin";
    public static bool IsValid(this string accessLevel) =>
        accessLevel == Guest || accessLevel == Member || accessLevel == Admin;

    public static bool IsMemberOrAdmin(this string accessLevel) =>
        accessLevel == Member || accessLevel == Admin;

    public static bool IsAdmin(this string accessLevel) =>
        accessLevel == Admin;
}