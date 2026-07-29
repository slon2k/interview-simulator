using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class GetInterview
{
    public static IEndpointRouteBuilder MapGetInterview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{interviewId:guid}", GetInterviewHandler)
            .WithName("GetInterview");

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

        ResponseQuestion? currentQuestion = null;

        if (interview.Status == InterviewStatus.Active)
        {
            var currentTurn = await store.GetTurnAsync(
                userId: userId,
                sessionId: interview.Id,
                turnNumber: interview.AnsweredCount + 1,
                cancellationToken: cancellationToken);

            if (currentTurn is not null)
            {
                var question = currentTurn.Question;
                currentQuestion = new ResponseQuestion(
                    Text: question.Text,
                    Topic: question.Topic);
            }
        }

        var feedback = interview.Feedback is not null
            ? new ResponseFeedback(
                Score: interview.Feedback.TotalScore,
                Summary: interview.Feedback.Summary)
            : null;

        return Results.Ok(new ResponseInterview(
            Id: interview.Id,
            UserId: interview.UserId,
            Status: interview.Status.ToString(),
            TargetRole: interview.TargetRole,
            FocusArea: interview.FocusArea,
            InterviewType: interview.InterviewType.ToString(),
            SeniorityLevel: interview.Seniority.ToString(),
            QuestionCount: interview.QuestionCount,
            AnsweredCount: interview.AnsweredCount,
            CreatedAt: interview.CreatedAt,
            CompletedAt: interview.CompletedAt,
            Feedback: feedback,
            CurrentQuestion: currentQuestion));
    }

    public record ResponseInterview(
        Guid Id,
        string UserId,
        string Status,
        string TargetRole,
        string FocusArea,
        string InterviewType,
        string SeniorityLevel,
        int QuestionCount,
        int AnsweredCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        ResponseFeedback? Feedback,
        ResponseQuestion? CurrentQuestion);

    public record ResponseQuestion(
        string Text,
        string Topic);

    public record ResponseFeedback(
        int Score,
        string? Summary);
}