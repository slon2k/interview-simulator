using System.Security.Claims;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class GenerateSummary
{
    public static void MapGenerateSummary(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{interviewId:guid}/summary", Handler)
            .WithName("GenerateSummary")
            .WithSummary("Generate a summary for an interview session")
            .WithDescription("Generates a summary for a completed interview session.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Handler(
        Guid interviewId,
        IInterviewStore store,
        SessionSummaryService sessionSummaryService,
        ClaimsPrincipal user,
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

        if (interview.Status != InterviewStatus.Completed)
        {
            return Errors.SessionNotCompleted.ToProblemResult();
        }

        if (interview.InterviewSummary is not null)
        {
            return Errors.SessionAlreadyHasSummary.ToProblemResult();
        }

        try
        {
            await sessionSummaryService.CreateSummaryAsync(interview.Id, userId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Session summary generation failed for interview {InterviewId}.",
                interview.Id);
            return Errors.SummaryGenerationFailed.ToProblemResult();
        }

        return Results.NoContent();
    }

    public static class Errors
    {
        public static Error SessionNotCompleted => Error.Conflict("Interviews.GenerateSummary.SessionNotCompleted", "Interview session is not completed.");
        public static Error SessionNotFound => Error.NotFound("Interviews.GenerateSummary.SessionNotFound", "Interview session not found.");
        public static Error Unauthorized => Error.Unauthorized("Interviews.GenerateSummary.Unauthorized", "Authentication is required.");
        public static Error SessionAlreadyHasSummary => Error.Conflict("Interviews.GenerateSummary.SessionAlreadyHasSummary", "Interview session already has a summary.");
        public static Error SummaryGenerationFailed => Error.Unexpected("Interviews.GenerateSummary.SummaryGenerationFailed", "Failed to generate summary for the interview session.");
    }
}