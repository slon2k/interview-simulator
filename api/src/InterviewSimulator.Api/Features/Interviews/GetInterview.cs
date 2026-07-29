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

        Question? currentQuestion = null;

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
                currentQuestion = new Question(
                    Text: question.Text,
                    Topic: question.Topic);
            }
        }

        var feedback = interview.Feedback is not null
            ? new Feedback(
                Score: interview.Feedback.TotalScore,
                Summary: interview.Feedback.Summary)
            : null;

        return Results.Ok(new Response(
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

    public record Response(
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
        Feedback? Feedback,
        Question? CurrentQuestion);

    public record Question(
        string Text,
        string Topic);

    public record Feedback(
        int Score,
        string? Summary);
}