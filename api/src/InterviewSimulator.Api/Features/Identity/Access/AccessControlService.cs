using System.Security.Claims;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Identity.Access;

public sealed class AccessControlService(IUserAccessReader userAccessReader, IOptions<AccessControlOptions> options) : IAccessControlService
{
    private readonly HashSet<string> _configuredAdminUserIds = new(
             options.Value.AdminUserIds ?? [],
            StringComparer.Ordinal);

    public async Task<AccessControlStatus> GetStatus(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return new AccessControlStatus(
                IsAuthenticated: false,
                UserId: null,
                IsInvited: false,
                IsAdmin: false);
        }

        var userId = user.FindFirstValue(AppClaimTypes.UserId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new AccessControlStatus(
                IsAuthenticated: true,
                UserId: null,
                IsInvited: false,
                IsAdmin: false);
        }

        if (_configuredAdminUserIds.Contains(userId))
        {
            return new AccessControlStatus(
                IsAuthenticated: true,
                UserId: userId,
                IsInvited: true,
                IsAdmin: true);
        }

        var userAccess = await userAccessReader.GetAccessByUserIdAsync(userId, cancellationToken);

        var isAdmin = IsAdmin(userAccess);
        var isInvited = IsInvited(userAccess);

        return new AccessControlStatus(
            IsAuthenticated: true,
            UserId: userId,
            IsInvited: isInvited,
            IsAdmin: isAdmin);
    }

    private static bool IsAdmin(UserAccessSnapshot? userAccess)
    {
        return userAccess?.IsDisabled != true && userAccess?.AccessLevel?.IsAdmin() == true;
    }

    private static bool IsInvited(UserAccessSnapshot? userAccess)
    {
        return userAccess?.IsDisabled != true && userAccess?.AccessLevel?.IsMemberOrAdmin() == true;
    }
}