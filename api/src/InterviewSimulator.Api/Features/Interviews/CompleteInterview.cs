using System.Security.Claims;

using InterviewSimulator.Api.Features.Common;
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
            return Unauthorized.ToProblemResult();
        }

        var interview = await store.GetSessionAsync(
            userId: userId,
            sessionId: interviewId,
            cancellationToken: cancellationToken);

        if (interview is null)
        {
            return SessionNotFound.ToProblemResult();
        }

        if (interview.Status != InterviewStatus.Active)
        {
            return SessionNotActive.ToProblemResult();
        }

        interview.Complete(timeProvider.GetUtcNow());

        await store.UpdateSessionAsync(interview, cancellationToken);

        return Results.NoContent();
    }

    public static Error SessionNotActive => Error.Conflict("Interviews.CompleteInterview.SessionNotActive", "Interview session is not active.");
    public static Error SessionNotFound => Error.NotFound("Interviews.CompleteInterview.SessionNotFound", "Interview session not found.");
    public static Error Unauthorized => Error.Unauthorized("Interviews.CompleteInterview.Unauthorized", "Authentication is required.");
}