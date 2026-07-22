using Microsoft.AspNetCore.Authorization;

namespace InterviewSimulator.Api.Features.Auth;

public static class AuthorizationPolicies
{
    public const string InvitedUser = "InvitedUser";
}

public sealed class InvitedUserRequirement : IAuthorizationRequirement;

public sealed class InvitedUserAuthorizationHandler(
    IAccessControlService accessControlService)
    : AuthorizationHandler<InvitedUserRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InvitedUserRequirement requirement)
    {
        var accessStatus = accessControlService.GetStatus(context.User);

        if (accessStatus.IsAuthenticated && accessStatus.IsInvited)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

