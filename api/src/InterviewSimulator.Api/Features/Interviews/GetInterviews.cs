using System.Security.Claims;

using FluentValidation;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

using Microsoft.AspNetCore.Mvc;

namespace InterviewSimulator.Api.Features.Interviews;

public static class GetInterviews
{
    public const int DefaultLimit = 100;
    public static IEndpointRouteBuilder MapGetInterviews(this IEndpointRouteBuilder endpoints)
    {
          endpoints.MapGet("/", Handler)
            .AddEndpointFilter<ValidationFilter<Request>>()
            .WithName("GetInterviews");

        return endpoints;
    }

    private static async Task<IResult> Handler(
        [AsParameters] Request request,
        IInterviewStore interviewStore,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Unauthorized.ToProblemResult();
        }

        IReadOnlyList<InterviewStatus>? statuses = request.Status is { Length: > 0 }
            ? request.Status.Select(s => Enum.Parse<InterviewStatus>(s, ignoreCase: true)).ToList()
            : null;

        var interviews = await interviewStore.ListSessionsAsync(
            userId: userId,
            statuses: statuses,
            limit: DefaultLimit,
            cancellationToken: cancellationToken);

        return Results.Ok(interviews.Select(MapToResponse));
    }

    public class Request
    {
        [FromQuery(Name = "status")]
        public string[]? Status { get; init; }
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
        int? TotalScore);

    private static Response MapToResponse(InterviewSession session)
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
            AnsweredCount: session.AnsweredCount,
            CreatedAt: session.CreatedAt,
            StartedAt: session.StartedAt,
            CompletedAt: session.CompletedAt,
            TotalScore: session.Feedback?.TotalScore);
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleForEach(x => x.Status)
                .IsEnumName(typeof(InterviewStatus), caseSensitive: false)
                .WithMessage("Invalid status filter. Allowed values: created, active, completed.")
                .When(x => x.Status is { Length: > 0 });
        }
    }

    public static Error Unauthorized => Error.Unauthorized("Interviews.GetInterviews.Unauthorized", "Authentication is required.");
}