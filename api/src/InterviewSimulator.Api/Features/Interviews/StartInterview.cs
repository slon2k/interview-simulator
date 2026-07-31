using System.Security.Claims;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class StartInterview
{
    public static void MapStartInterview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{interviewId:guid}/start", Handler)
            .WithName("StartInterview");
    }

    private static async Task<IResult> Handler(
        Guid interviewId,
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

        var session = await store.GetSessionAsync(
            userId: userId,
            sessionId: interviewId,
            cancellationToken: cancellationToken);

        if (session is null)
        {
            return Errors.SessionNotFound.ToProblemResult();
        }

        if (session.Status != InterviewStatus.Created)
        {
            return Errors.SessionNotCreated.ToProblemResult();
        }

        session.Start(timeProvider.GetUtcNow());

        var generateQuestionRequest = new GenerateQuestionRequest(
                TargetRole: session.TargetRole,
                Seniority: session.Seniority,
                FocusArea: session.FocusArea,
                TurnNumber: 1,
                QuestionCount: session.QuestionCount,
                PreviousTurns: [],
                InterviewType: session.InterviewType);

        var question = await questionGenerator.GenerateQuestionAsync(
            generateQuestionRequest,
                cancellationToken);

        if (question is null)
        {
            return Errors.NextQuestionGenerationFailed.ToProblemResult();
        }

        var turn = InterviewTurn.Create(
            sessionId: session.Id,
            userId: session.UserId,
            turnNumber: 1,
            question: new InterviewQuestion(
                text: question.Text,
                topic: question.Topic),
            createdAt: timeProvider.GetUtcNow());

        await store.StartInterviewAsync(session, turn, cancellationToken);

        return Results.Ok(MapToResponse(session, turn));
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
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        Question CurrentQuestion);

    public record Question(
        string Text,
        string Topic);

    private static Response MapToResponse(InterviewSession session, InterviewTurn turn) => new(
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
        CurrentQuestion: new Question(
            Text: turn.Question.Text,
            Topic: turn.Question.Topic));
    
    public static class Errors
    {
        public static Error Unauthorized => Error.Unauthorized("Interviews.StartInterview.Unauthorized", "Authentication is required.");
        public static Error SessionNotFound => Error.NotFound("Interviews.StartInterview.SessionNotFound", "Interview session not found.");
        public static Error SessionNotCreated => Error.Conflict("Interviews.StartInterview.SessionNotCreated", "Interview session is not in a created state.");
        public static Error NextQuestionGenerationFailed => Error.Unexpected("Interviews.StartInterview.NextQuestionGenerationFailed", "Failed to generate next question.");
    }
}