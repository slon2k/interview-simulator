using InterviewSimulator.Api.Features.Identity.Authorization;

namespace InterviewSimulator.Api.Features.Interviews;

public static class InterviewEndpoints
{
    public static IEndpointRouteBuilder MapInterviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/interviews")
            .RequireAuthorization(AuthorizationPolicies.InvitedUser)
            .WithTags("Interviews");

        group.MapGetInterview();
        group.MapGetInterviews();
        group.MapCreateInterview();
        group.MapStartInterview();
        group.MapSubmitAnswer();
        group.MapCompleteInterview();
        group.MapGetInterviewDetails();
        group.MapGenerateSummary();

        return endpoints;
    }
}