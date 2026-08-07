using System.Security.Claims;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class GetInterview
{
    public static IEndpointRouteBuilder MapGetInterview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{interviewId:guid}", GetInterviewHandler)
            .WithName("GetInterview")
            .WithSummary("Get an interview session")
            .WithDescription("Returns the interview session with the given ID belonging to the authenticated user.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<IResult> GetInterviewHandler(
        Guid interviewId,
        IInterviewStore store,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
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

        QuestionResponse? currentQuestion = null;

        if (interview.Status == InterviewStatus.Active)
        {
            int turnNumber = interview.AnsweredCount + 1;
            var currentTurn = await store.GetTurnAsync(
                userId: userId,
                sessionId: interview.Id,
                turnNumber: turnNumber,
                cancellationToken: cancellationToken);

            if (currentTurn is not null)
            {
                var question = currentTurn.Question;
                currentQuestion = new QuestionResponse(
                    Text: question.Text,
                    Topic: question.Topic,
                    TurnNumber: turnNumber);
            }
        }

        var feedback = interview.Feedback is not null
            ? new FeedbackResponse(
                Score: interview.Feedback.Score,
                Summary: interview.Feedback.Summary)
            : null;

        return Results.Ok(new Response(
            Id: interview.Id,
            UserId: interview.UserId,
            Status: interview.Status.ToContract(),
            TargetRole: interview.TargetRole,
            FocusArea: interview.FocusArea,
            InterviewType: interview.InterviewType.ToContract(),
            SeniorityLevel: interview.Seniority.ToContract(),
            QuestionCount: interview.QuestionCount,
            AnsweredCount: interview.AnsweredCount,
            CreatedAt: interview.CreatedAt,
            StartedAt: interview.StartedAt,
            CompletedAt: interview.CompletedAt,
            Feedback: feedback,
            CurrentQuestion: currentQuestion));
    }

    public record Response(
        Guid Id,
        string UserId,
        InterviewStatusContract Status,
        string TargetRole,
        string FocusArea,
        InterviewTypeContract InterviewType,
        SeniorityLevelContract SeniorityLevel,
        int QuestionCount,
        int AnsweredCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        FeedbackResponse? Feedback,
        QuestionResponse? CurrentQuestion);

    public static class Errors
    {
        public static Error SessionNotFound => Error.NotFound("Interviews.GetInterview.SessionNotFound", "Interview session not found.");
        public static Error Unauthorized => Error.Unauthorized("Interviews.GetInterview.Unauthorized", "Authentication is required.");
    }
}