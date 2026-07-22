using System.Security.Claims;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Auth;

public sealed class ConfigAccessControlService : IAccessControlService
{
    private readonly HashSet<string> _invitedUserIds;
    private readonly HashSet<string> _adminUserIds;

    public ConfigAccessControlService(IOptions<AccessControlOptions> options)
    {
        var accessControlOptions = options.Value;

        _invitedUserIds = new HashSet<string>(
            accessControlOptions.InvitedUserIds ?? [],
            StringComparer.Ordinal);

        _adminUserIds = new HashSet<string>(
            accessControlOptions.AdminUserIds ?? [],
            StringComparer.Ordinal);
    }

    public AccessControlStatus GetStatus(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return new AccessControlStatus(
                IsAuthenticated: false,
                UserId: null,
                IsInvited: false,
                IsAdmin: false);
        }

        var userId =
            user.FindFirstValue(AppClaimTypes.UserId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        var isAdmin = IsAdmin(userId);
        var isInvited = IsInvited(userId);

        return new AccessControlStatus(
            IsAuthenticated: true,
            UserId: userId,
            IsInvited: isInvited,
            IsAdmin: isAdmin);
    }

    public bool IsInvited(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        // Admins are always treated as invited.
        return _invitedUserIds.Contains(userId) || _adminUserIds.Contains(userId);
    }

    public bool IsAdmin(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        return _adminUserIds.Contains(userId);
    }
}