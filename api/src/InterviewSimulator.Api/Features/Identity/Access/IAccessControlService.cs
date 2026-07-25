using System.Security.Claims;

namespace InterviewSimulator.Api.Features.Identity.Access;

public interface IAccessControlService
{
    Task<AccessControlStatus> GetStatusAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public sealed record AccessControlStatus(
    bool IsAuthenticated,
    string? UserId,
    bool IsInvited,
    bool IsAdmin);