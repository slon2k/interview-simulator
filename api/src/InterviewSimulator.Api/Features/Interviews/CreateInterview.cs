using System.Security.Claims;

using FluentValidation;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class CreateInterview
{
    public static IEndpointRouteBuilder MapCreateInterview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", Handler)
            .AddEndpointFilter<ValidationFilter<Request>>()
            .WithName("CreateInterview");

        return endpoints;
    }

    private static async Task<IResult> Handler(
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

        if (!Enum.TryParse<InterviewType>(request.InterviewType, ignoreCase: true, out var parsedType))
        {
            return Results.BadRequest(new { error = $"Invalid interview type: {request.InterviewType}" });
        }

        if (!Enum.TryParse<SeniorityLevel>(request.SeniorityLevel, ignoreCase: true, out var parsedSeniority))
        {
            return Results.BadRequest(new { error = $"Invalid seniority level: {request.SeniorityLevel}" });
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
        Question CurrentQuestion);

    public record Question(
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
            CurrentQuestion: new Question(
                Text: firstTurn.Question.Text,
                Topic: firstTurn.Question.Topic));
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TargetRole).NotEmpty().WithMessage("Target role is required.");
            RuleFor(x => x.FocusArea).NotEmpty().WithMessage("Focus area is required.");

            RuleFor(x => x.InterviewType)
                .NotEmpty().WithMessage("Interview type is required.")
                .Must(value => Enum.TryParse<InterviewType>(value, ignoreCase: true, out _))
                .WithMessage(value => $"Invalid interview type: {value.InterviewType}");

            RuleFor(x => x.SeniorityLevel)
                .NotEmpty().WithMessage("Seniority level is required.")
                .Must(value => Enum.TryParse<SeniorityLevel>(value, ignoreCase: true, out _))
                .WithMessage(value => $"Invalid seniority level: {value.SeniorityLevel}");

            RuleFor(x => x.QuestionCount).GreaterThan(0).WithMessage("Question count must be greater than zero.");
        }
    }
}