using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class CompleteInterview
{
    public static void MapCompleteInterview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{interviewId:guid}/complete", Handler)
            .WithName("CompleteInterview");
    }

    private static async Task<IResult> Handler(
        Guid interviewId,
        IInterviewStore store,
        ClaimsPrincipal user,
        TimeProvider timeProvider,
        CancellationToken cancellationToken
    )
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Results.Unauthorized();
        }

        var interview = await store.GetSessionAsync(
            userId: userId,
            sessionId: interviewId,
            cancellationToken: cancellationToken);

        if (interview is null)
        {
            return Results.NotFound();
        }

        if (interview.Status != InterviewStatus.Active)
        {
            return Results.Conflict(new { error = "Interview session is not active." });
        }

        interview.Complete(timeProvider.GetUtcNow());

        await store.SaveSessionAsync(interview, cancellationToken);

        return Results.NoContent();
    }
}