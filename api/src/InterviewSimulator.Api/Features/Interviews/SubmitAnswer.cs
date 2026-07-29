using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class SubmitAnswer
{
    public record Request(int TurnNumber, string Answer);

    public static IEndpointRouteBuilder MapSubmitAnswer(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{sessionId:guid}/answers", SubmitAnswerHandler)
            .WithName("SubmitAnswer");

        return endpoints;
    }

    public static async Task<IResult> SubmitAnswerHandler(
        Guid sessionId,
        Request request,
        IInterviewStore store,
        IQuestionGenerator questionGenerator,
        ClaimsPrincipal user,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Results.Unauthorized();
        }

        if (await store.GetSessionAsync(userId, sessionId, cancellationToken) is not InterviewSession session)
        {
            return Results.NotFound();
        }

        if (session.Status != InterviewStatus.Active)
        {
            return Results.Conflict(new { error = "Interview session is not active." });
        }

        if (request.TurnNumber != session.AnsweredCount + 1)
        {
            return Results.Conflict(new { error = "Invalid turn number." });
        }

        if (await store.GetTurnAsync(userId, sessionId, request.TurnNumber, cancellationToken) is not InterviewTurn currentTurn)
        {
            return Results.NotFound();
        }
        var now = timeProvider.GetUtcNow();

        try
        {
            session.RecordAnswer(now);
            currentTurn.RecordAnswer(request.Answer, now);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }

        InterviewTurn? nextTurn = null;

        if (session.Status == InterviewStatus.Active)
        {
            var previousTurns = await store.ListTurnsAsync(userId, sessionId, cancellationToken);
            var nextTurnNumber = session.AnsweredCount + 1;
            var generateQuestionRequest = new GenerateQuestionRequest(
                TargetRole: session.TargetRole,
                Seniority: session.Seniority,
                FocusArea: session.FocusArea,
                TurnNumber: nextTurnNumber,
                QuestionCount: session.QuestionCount,
                PreviousTurns: previousTurns.Select(turn => new PreviousInterviewTurn(
                    TurnNumber: turn.TurnNumber,
                    QuestionText: turn.Question.Text,
                    QuestionTopic: turn.Question.Topic,
                    AnswerText: turn.Answer?.Text ?? string.Empty)).ToList(),
                InterviewType: session.InterviewType);

            var nextQuestion = await questionGenerator.GenerateQuestionAsync(
                generateQuestionRequest,
                cancellationToken);

            if (nextQuestion is null)
            {
                return Results.InternalServerError(new { error = "Failed to generate next question." });
            }

            nextTurn = InterviewTurn.Create(
                sessionId: session.Id,
                turnNumber: nextTurnNumber,
                question: new InterviewQuestion(
                    text: nextQuestion.Text,
                    topic: nextQuestion.Topic),
                userId: userId,
                createdAt: now);
        }

        await store.SaveAnswerSubmissionAsync(
            session: session,
            answeredTurn: currentTurn,
            nextTurn: nextTurn,
            cancellationToken: cancellationToken);

        return Results.Ok();
    }
}