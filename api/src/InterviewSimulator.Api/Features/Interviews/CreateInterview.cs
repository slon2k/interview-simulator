using System.Security.Claims;

using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class CreateInterview
{
    public static IEndpointRouteBuilder MapCreateInterview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", CreateInterviewHandler)
            .WithName("CreateInterview");

        return endpoints;
    }

    private static async Task<IResult> CreateInterviewHandler(
        Request request,
        IInterviewStore store,
        IQuestionGenerator questionGenerator,
        ClaimsPrincipal user,
        TimeProvider timeProvider,
        CancellationToken cancellationToken
    )
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Results.Unauthorized();
        }

        if (Enum.TryParse<SeniorityLevel>(request.SeniorityLevel, ignoreCase: true, out var parsedSeniority) is false)
        {
            return Results.BadRequest(new { error = $"Invalid seniority level: {request.SeniorityLevel}" });
        }

        if (Enum.TryParse<InterviewType>(request.InterviewType, ignoreCase: true, out var parsedType) is false)
        {
            return Results.BadRequest(new { error = $"Invalid interview type: {request.InterviewType}" });
        }

        if (request.QuestionCount <= 0)
        {
            return Results.BadRequest(new { error = "Question count must be greater than zero." });
        }

        var question = await questionGenerator.GenerateQuestionAsync(
            new GenerateQuestionRequest(
                TargetRole: request.TargetRole,
                FocusArea: request.FocusArea,
                InterviewType: parsedType,
                Seniority: parsedSeniority,
                TurnNumber: 1,
                QuestionCount: request.QuestionCount,
                PreviousTurns: []),
            cancellationToken);

        if (question is null)
        {
            return Results.InternalServerError(new { error = "Failed to generate question." });
        }

        var now = timeProvider.GetUtcNow();

        var interviewSession = InterviewSession.Create(
            userId: userId,
            targetRole: request.TargetRole,
            focusArea: request.FocusArea,
            seniority: parsedSeniority,
            interviewType: parsedType,
            questionCount: request.QuestionCount,
            createdAt: now);

        var firstTurn = InterviewTurn.Create(
            sessionId: interviewSession.Id,
            userId: userId,
            turnNumber: 1,
            question: new InterviewQuestion(
                text: question.Text,
                topic: question.Topic),
            createdAt: now);

        interviewSession.Start(now);

        await store.CreateInterviewAsync(interviewSession, firstTurn, cancellationToken);

        return Results.Created(
            $"/api/interviews/{interviewSession.Id}",
            MapToResponse(interviewSession, firstTurn));
    }

    public record Request(
        string TargetRole,
        string FocusArea,
        string InterviewType,
        string SeniorityLevel,
        int QuestionCount);

    public record Response(
        Guid Id,
        string UserId,
        string Status,
        string TargetRole,
        string FocusArea,
        string InterviewType,
        string SeniorityLevel,
        int QuestionCount,
        DateTimeOffset CreatedAt,
        ResponseQuestion CurrentQuestion);

    public record ResponseQuestion(
        string Text,
        string Topic);

    private static Response MapToResponse(
        InterviewSession session,
        InterviewTurn firstTurn)
    {
        return new Response(
            Id: session.Id,
            UserId: session.UserId,
            Status: session.Status.ToString(),
            TargetRole: session.TargetRole,
            FocusArea: session.FocusArea,
            InterviewType: session.InterviewType.ToString(),
            SeniorityLevel: session.Seniority.ToString(),
            QuestionCount: session.QuestionCount,
            CreatedAt: session.CreatedAt,
            CurrentQuestion: new ResponseQuestion(
                Text: firstTurn.Question.Text,
                Topic: firstTurn.Question.Topic));
    }
}