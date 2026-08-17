using System.Security.Claims;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class CompleteInterview
{
    public static void MapCompleteInterview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{interviewId:guid}/complete", Handler)
            .WithName("CompleteInterview")
            .WithSummary("Complete an interview session")
            .WithDescription("Transitions an active interview session to the Completed state.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handler(
        Guid interviewId,
        IInterviewStore store,
        SessionSummaryService sessionSummaryService,
        ClaimsPrincipal user,
        TimeProvider timeProvider,
        ILogger<Program> logger,
        CancellationToken cancellationToken
    )
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Errors.Unauthorized.ToProblemResult();
        }

        var interview = await store.GetSessionAsync(
            userId: userId,
            sessionId: interviewId,
            cancellationToken: cancellationToken);

        if (interview is null)
        {
            return Errors.SessionNotFound.ToProblemResult();
        }

        if (interview.Status != InterviewStatus.Active)
        {
            return Errors.SessionNotActive.ToProblemResult();
        }

        interview.Complete(timeProvider.GetUtcNow());

        await store.UpdateSessionAsync(interview, cancellationToken);

        try
        {
            interview = await sessionSummaryService.CreateSummaryAsync(interview.Id, userId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Session summary generation failed for interview {InterviewId}. The completed session remains available without a summary.",
                interview.Id);
        }

        return Results.NoContent();
    }

    public static class Errors
    {
        public static Error SessionNotActive => Error.Conflict("Interviews.CompleteInterview.SessionNotActive", "Interview session is not active.");
        public static Error SessionNotFound => Error.NotFound("Interviews.CompleteInterview.SessionNotFound", "Interview session not found.");
        public static Error Unauthorized => Error.Unauthorized("Interviews.CompleteInterview.Unauthorized", "Authentication is required.");
    }
}