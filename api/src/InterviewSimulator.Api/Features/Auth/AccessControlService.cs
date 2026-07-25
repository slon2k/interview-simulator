using System.Security.Claims;

using InterviewSimulator.Api.Features.Users;
using InterviewSimulator.Api.Infrastructure.Data;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Auth;

public sealed class AccessControlService(IRepository<UserDocument> userRepository, IOptions<AccessControlOptions> options) : IAccessControlService
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

        var userDocument = await userRepository.GetByIdAsync(userId, userId, cancellationToken);

        var isAdmin = IsAdmin(userDocument);
        var isInvited = IsInvited(userDocument);

        return new AccessControlStatus(
            IsAuthenticated: true,
            UserId: userId,
            IsInvited: isInvited,
            IsAdmin: isAdmin);
    }

    private static bool IsAdmin(UserDocument? userDocument)
    {
        return userDocument?.IsDisabled != true && userDocument?.AccessLevel?.IsAdmin() == true;
    }

    private static bool IsInvited(UserDocument? userDocument)
    {
        return userDocument?.IsDisabled != true && userDocument?.AccessLevel?.IsMemberOrAdmin() == true;
    }
}