using System.Security.Claims;

namespace InterviewSimulator.Api.Features.Auth;

public interface IAccessControlService
{
    Task<AccessControlStatus> GetStatus(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}