using System.Security.Claims;

namespace InterviewSimulator.Api.Features.Auth;

public interface IAccessControlService
{
    AccessControlStatus GetStatus(ClaimsPrincipal user);

    bool IsInvited(string? userId);

    bool IsAdmin(string? userId);
}