using System.Security.Claims;

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
        string? status,
        IInterviewStore interviewStore,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Results.Unauthorized();
        }

        if (!TryParseStatus(status, out var parsedStatus, out var errorMessage))
        {
            return Results.BadRequest(new { error = errorMessage });
        }

        var interviews = await interviewStore.ListSessionsAsync(
            userId: userId,
            status: parsedStatus,
            limit: DefaultLimit,
            cancellationToken: cancellationToken);

        return Results.Ok(interviews.Select(MapToResponse));
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

    private static bool TryParseStatus(
        string? rawStatus,
        out InterviewStatus? status,
        out string? errorMessage)
    {
        status = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return true;
        }

        if (!Enum.TryParse<InterviewStatus>(rawStatus, ignoreCase: true, out var parsed))
        {
            errorMessage = "Invalid status filter. Allowed values: active, completed.";
            return false;
        }

        if (parsed is not InterviewStatus.Active and not InterviewStatus.Completed)
        {
            errorMessage = "Invalid status filter. Allowed values: active, completed.";
            return false;
        }

        status = parsed;
        return true;
    }
}