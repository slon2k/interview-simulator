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
            .WithName("CreateInterview")
            .WithSummary("Create an interview session")
            .WithDescription("Creates a new interview session in the Created state for the authenticated user.")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> Handler(
        Request request,
        IInterviewStore store,
        ClaimsPrincipal user,
        TimeProvider timeProvider,
        CancellationToken cancellationToken
    )
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Errors.Unauthorized.ToProblemResult();
        }

        var now = timeProvider.GetUtcNow();

        var interviewSession = InterviewSession.Create(
            userId: userId,
            targetRole: request.TargetRole,
            focusArea: request.FocusArea,
            seniority: request.SeniorityLevel.ToDomain(),
            interviewType: request.InterviewType.ToDomain(),
            questionCount: request.QuestionCount,
            createdAt: now);

        await store.CreateSessionAsync(interviewSession, cancellationToken);

        return Results.Created(
            $"/api/interviews/{interviewSession.Id}",
            MapToResponse(interviewSession));
    }

    public record Request(
        string TargetRole,
        string FocusArea,
        InterviewTypeContract InterviewType,
        SeniorityLevelContract SeniorityLevel,
        int QuestionCount);

    public record Response(
        Guid Id,
        string UserId,
        string Status,
        string TargetRole,
        string FocusArea,
        InterviewTypeContract InterviewType,
        SeniorityLevelContract SeniorityLevel,
        int QuestionCount,
        DateTimeOffset CreatedAt);

    private static Response MapToResponse(
        InterviewSession session) => new(
            Id: session.Id,
            UserId: session.UserId,
            Status: session.Status.ToString(),
            TargetRole: session.TargetRole,
            FocusArea: session.FocusArea,
            InterviewType: session.InterviewType.ToContract(),
            SeniorityLevel: session.Seniority.ToContract(),
            QuestionCount: session.QuestionCount,
            CreatedAt: session.CreatedAt);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TargetRole).NotEmpty().WithMessage("Target role is required.");
            RuleFor(x => x.FocusArea).NotEmpty().WithMessage("Focus area is required.");
            RuleFor(x => x.InterviewType).IsInEnum().WithMessage("Interview type is invalid.");
            RuleFor(x => x.SeniorityLevel).IsInEnum().WithMessage("Seniority level is invalid.");
            RuleFor(x => x.QuestionCount).GreaterThan(0).WithMessage("Question count must be greater than zero.");
        }
    }

    public static class Errors
    {
        public static Error Unauthorized => Error.Unauthorized("Interviews.CreateInterview.Unauthorized", "Authentication is required.");
    }
}