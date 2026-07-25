using InterviewSimulator.Api.Features.Identity.Access;

using Microsoft.AspNetCore.Authorization;

namespace InterviewSimulator.Api.Features.Identity.Authorization;

public static class AuthorizationPolicies
{
    public const string InvitedUser = "InvitedUser";
}

public sealed class InvitedUserRequirement : IAuthorizationRequirement;

public sealed class InvitedUserAuthorizationHandler(
    IAccessControlService accessControlService)
    : AuthorizationHandler<InvitedUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InvitedUserRequirement requirement)
    {
        var accessStatus = await accessControlService.GetStatusAsync(context.User);

        if (accessStatus.IsAuthenticated && accessStatus.IsInvited)
        {
            context.Succeed(requirement);
        }
    }
}

