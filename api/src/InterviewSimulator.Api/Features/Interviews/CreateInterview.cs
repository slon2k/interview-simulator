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
        ClaimsPrincipal user,
        TimeProvider timeProvider,
        CancellationToken cancellationToken
    )
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Results.Unauthorized();
        }

        var interviewType = Enum.Parse<InterviewType>(request.InterviewType, ignoreCase: true);
        var seniorityLevel = Enum.Parse<SeniorityLevel>(request.SeniorityLevel, ignoreCase: true);

        var now = timeProvider.GetUtcNow();

        var interviewSession = InterviewSession.Create(
            userId: userId,
            targetRole: request.TargetRole,
            focusArea: request.FocusArea,
            seniority: seniorityLevel,
            interviewType: interviewType,
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
        DateTimeOffset CreatedAt);

    private static Response MapToResponse(
        InterviewSession session) => new(
            Id: session.Id,
            UserId: session.UserId,
            Status: session.Status.ToString(),
            TargetRole: session.TargetRole,
            FocusArea: session.FocusArea,
            InterviewType: session.InterviewType.ToString(),
            SeniorityLevel: session.Seniority.ToString(),
            QuestionCount: session.QuestionCount,
            CreatedAt: session.CreatedAt);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TargetRole).NotEmpty().WithMessage("Target role is required.");
            RuleFor(x => x.FocusArea).NotEmpty().WithMessage("Focus area is required.");

            RuleFor(x => x.InterviewType)
                .NotEmpty().WithMessage("Interview type is required.")
                .IsEnumName(typeof(InterviewType), caseSensitive: false)
                .WithMessage(value => $"Invalid interview type: {value.InterviewType}");

            RuleFor(x => x.SeniorityLevel)
                .NotEmpty().WithMessage("Seniority level is required.")
                .IsEnumName(typeof(SeniorityLevel), caseSensitive: false)
                .WithMessage(value => $"Invalid seniority level: {value.SeniorityLevel}");

            RuleFor(x => x.QuestionCount).GreaterThan(0).WithMessage("Question count must be greater than zero.");
        }
    }
}