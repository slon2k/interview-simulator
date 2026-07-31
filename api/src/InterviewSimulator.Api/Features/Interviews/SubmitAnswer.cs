using System.Security.Claims;

using FluentValidation;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class SubmitAnswer
{
    public record Request(int TurnNumber, string Answer);

    public static IEndpointRouteBuilder MapSubmitAnswer(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{sessionId:guid}/answers", SubmitAnswerHandler)
            .AddEndpointFilter<ValidationFilter<Request>>()
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
            return Errors.Unauthorized.ToProblemResult();
        }

        if (await store.GetSessionAsync(userId, sessionId, cancellationToken) is not InterviewSession session)
        {
            return Errors.SessionNotFound.ToProblemResult();
        }

        if (session.Status != InterviewStatus.Active)
        {
            return Errors.SessionNotActive.ToProblemResult();
        }

        if (request.TurnNumber != session.AnsweredCount + 1)
        {
            return Errors.InvalidTurnNumber.ToProblemResult();
        }

        if (await store.GetTurnAsync(userId, sessionId, request.TurnNumber, cancellationToken) is not InterviewTurn currentTurn)
        {
            return Errors.TurnNotFound.ToProblemResult();
        }
        var now = timeProvider.GetUtcNow();

        session.RecordAnswer(now);
        currentTurn.RecordAnswer(request.Answer, now);

        InterviewTurn? nextTurn = null;

        if (session.Status == InterviewStatus.Active)
        {
            var previousTurns = await store.ListTurnsAsync(userId, sessionId, cancellationToken);
            var nextTurnNumber = session.AnsweredCount + 1;
            var turnsForGeneration = previousTurns
                .Where(turn => turn.TurnNumber != currentTurn.TurnNumber)
                .Append(currentTurn)
                .OrderBy(turn => turn.TurnNumber)
                .ToArray();

            var generateQuestionRequest = new GenerateQuestionRequest(
                TargetRole: session.TargetRole,
                Seniority: session.Seniority,
                FocusArea: session.FocusArea,
                TurnNumber: nextTurnNumber,
                QuestionCount: session.QuestionCount,
                PreviousTurns: [.. turnsForGeneration.Select(turn => new PreviousInterviewTurn(
                    TurnNumber: turn.TurnNumber,
                    QuestionText: turn.Question.Text,
                    QuestionTopic: turn.Question.Topic,
                    AnswerText: turn.Answer?.Text ?? string.Empty))],
                InterviewType: session.InterviewType);

            var nextQuestion = await questionGenerator.GenerateQuestionAsync(
                generateQuestionRequest,
                cancellationToken);

            if (nextQuestion is null)
            {
                return Errors.NextQuestionGenerationFailed.ToProblemResult();
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

        await store.SaveAnswerAsync(
            session: session,
            answeredTurn: currentTurn,
            nextTurn: nextTurn,
            cancellationToken: cancellationToken);

        return Results.Ok(MapToResponse(session, nextTurn));
    }

    private static Response MapToResponse(InterviewSession session, InterviewTurn? nextTurn) => new(
        Id: session.Id,
        UserId: session.UserId,
        Status: session.Status.ToString(),
        TargetRole: session.TargetRole,
        FocusArea: session.FocusArea,
        InterviewType: session.InterviewType.ToString(),
        SeniorityLevel: session.Seniority.ToString(),
        QuestionCount: session.QuestionCount,
        AnsweredCount: session.AnsweredCount,
        CreatedAt: session.CreatedAt,
        StartedAt: session.StartedAt,
        CompletedAt: session.CompletedAt,
        Feedback: session.Feedback is not null
            ? new Feedback(
                Score: session.Feedback.TotalScore,
                Summary: session.Feedback.Summary)
            : null,
        CurrentQuestion: nextTurn is not null
            ? new Question(
                Text: nextTurn.Question.Text,
                Topic: nextTurn.Question.Topic)
            : null);

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
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        Feedback? Feedback,
        Question? CurrentQuestion);

    public record Question(
        string Text,
        string Topic);

    public record Feedback(
        int Score,
        string? Summary);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TurnNumber)
                .GreaterThan(0)
                .WithMessage("Turn number must be greater than 0.");

            RuleFor(x => x.Answer)
                .NotEmpty()
                .WithMessage("Answer cannot be empty.");
        }
    }

    public static class Errors
    {
        public static Error Unauthorized => Error.Unauthorized("Interviews.SubmitAnswer.Unauthorized", "Authentication is required.");
        public static Error SessionNotFound => Error.NotFound("Interviews.SubmitAnswer.SessionNotFound", "Interview session not found.");
        public static Error SessionNotActive => Error.Conflict("Interviews.SubmitAnswer.SessionNotActive", "Interview session is not active.");
        public static Error InvalidTurnNumber => Error.Conflict("Interviews.SubmitAnswer.InvalidTurnNumber", "Invalid turn number.");
        public static Error TurnNotFound => Error.NotFound("Interviews.SubmitAnswer.TurnNotFound", "Interview turn not found.");
        public static Error NextQuestionGenerationFailed => Error.Unexpected("Interviews.SubmitAnswer.NextQuestionGenerationFailed", "Failed to generate next question.");
    }
}