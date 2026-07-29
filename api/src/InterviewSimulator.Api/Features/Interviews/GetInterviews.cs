using System.Security.Claims;

using FluentValidation;

using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class GetInterviews
{
    public const int DefaultLimit = 100;
    public static IEndpointRouteBuilder MapGetInterviews(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", Handler)
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
            return Results.Unauthorized();
        }

        InterviewStatus? status = string.IsNullOrWhiteSpace(request.Status) is false
            ? Enum.Parse<InterviewStatus>(request.Status, ignoreCase: true)
            : null;

        var interviews = await interviewStore.ListSessionsAsync(
            userId: userId,
            status: status,
            limit: DefaultLimit,
            cancellationToken: cancellationToken);

        return Results.Ok(interviews.Select(MapToResponse));
    }

    public record Request(string? Status);

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
            CompletedAt: session.CompletedAt,
            TotalScore: session.Feedback?.TotalScore);
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Status)
                .IsEnumName(typeof(AllowedStatus), caseSensitive: false)
                .WithMessage("Invalid status filter. Allowed values: active, completed.")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));
        }

        private enum AllowedStatus
        {
            Active,
            Completed
        }
    }
}