using System.Security.Claims;

using InterviewSimulator.Api.Features.Common;
using InterviewSimulator.Api.Features.Identity;

namespace InterviewSimulator.Api.Features.Interviews;

public static class GetInterviewDetails
{
    public static IEndpointRouteBuilder MapGetInterviewDetails(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{interviewId:guid}/details", Handler)
            .WithName("GetInterviewDetails")
            .WithSummary("Get an interview session with turns and summary")
            .WithDescription("Returns the interview session details with the given ID belonging to the authenticated user.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> Handler(
        Guid interviewId,
        IInterviewStore store,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (IdentityClaims.GetUserId(user) is not string userId)
        {
            return Errors.Unauthorized.ToProblemResult();
        }

        var interview = await store.GetSessionAsync(
            userId: userId,
            sessionId: interviewId,
            cancellationToken: cancellationToken);

        if (interview is null)
        {
            return Errors.SessionNotFound.ToProblemResult();
        }

        var turns = await store.ListTurnsAsync(
            userId: userId,
            sessionId: interviewId,
            cancellationToken: cancellationToken);

        var totalScore = interview.Status == InterviewStatus.Completed
            ? interview.SessionResult?.OverallScore
            : null;

        return Results.Ok(new Response(
            Id: interview.Id,
            UserId: interview.UserId,
            Status: interview.Status.ToContract(),
            TargetRole: interview.TargetRole,
            FocusArea: interview.FocusArea,
            InterviewType: interview.InterviewType.ToContract(),
            SeniorityLevel: interview.Seniority.ToContract(),
            QuestionCount: interview.QuestionCount,
            AnsweredCount: interview.AnsweredCount,
            CreatedAt: interview.CreatedAt,
            StartedAt: interview.StartedAt,
            CompletedAt: interview.CompletedAt,
            TotalScore: totalScore,
            Summary: ResponseSummary.FromDomain(interview.InterviewSummary),
            Turns: [.. turns.Select(turn => ResponseTurn.FromDomain(turn, interview.Status))]));
    }

    public record Response(
        Guid Id,
        string UserId,
        InterviewStatusContract Status,
        string TargetRole,
        string FocusArea,
        InterviewTypeContract InterviewType,
        SeniorityLevelContract SeniorityLevel,
        int QuestionCount,
        int AnsweredCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        int? TotalScore,
        ResponseSummary? Summary,
        IReadOnlyList<ResponseTurn> Turns);

    public record ResponseTurn(
        int TurnNumber,
        ResponseQuestion Question,
        ResponseAnswer? Answer,
        ResponseEvaluation? Evaluation,
        DateTimeOffset CreatedAt)
    {
        public static ResponseTurn FromDomain(InterviewTurn turn, InterviewStatus status) => new(
            TurnNumber: turn.TurnNumber,
            CreatedAt: turn.CreatedAt,
            Question: ResponseQuestion.FromDomain(turn.Question),
            Answer: ResponseAnswer.FromDomain(turn.Answer),
            Evaluation: status == InterviewStatus.Completed ? ResponseEvaluation.FromDomain(turn.Evaluation) : null);
    };

    public record ResponseSummary(string Text, DateTimeOffset CreatedAt)
    {
        public static ResponseSummary? FromDomain(InterviewSummary? summary) => summary is not null ? new(
            Text: summary.Text,
            CreatedAt: summary.CreatedAt) : null;
    };

    public record ResponseAnswer(string Text, DateTimeOffset CreatedAt)
    {
        public static ResponseAnswer? FromDomain(InterviewAnswer? answer) => answer is not null ? new(
            Text: answer.Text,
            CreatedAt: answer.AnsweredAt) : null;
    };

    public record ResponseQuestion(string Text, string Topic)
    {
        public static ResponseQuestion FromDomain(InterviewQuestion question) => new(
            Text: question.Text,
            Topic: question.Topic);
    };

    public record ResponseEvaluation(
        int OverallScore,
        string OverallFeedback,
        IReadOnlyList<ResponseEvaluationDimension> Dimensions)
    {
        public static ResponseEvaluation? FromDomain(AnswerEvaluation? evaluation) => evaluation is not null
            ? new(
                OverallScore: evaluation.OverallScore,
                OverallFeedback: evaluation.Feedback,
                Dimensions: [.. evaluation.Dimensions.Select(ResponseEvaluationDimension.FromDomain)])
            : null;
    };

    public record ResponseEvaluationDimension(
        string Key,
        string Label,
        int Score,
        string Feedback)
    {
        public static ResponseEvaluationDimension FromDomain(EvaluationDimension dimension) => new(
            Key: dimension.Key,
            Label: dimension.Label,
            Score: dimension.Score,
            Feedback: dimension.Feedback);
    };

    public static class Errors
    {
        public static Error SessionNotFound => Error.NotFound("Interviews.GetInterviewDetails.SessionNotFound", "Interview session not found.");
        public static Error Unauthorized => Error.Unauthorized("Interviews.GetInterviewDetails.Unauthorized", "Authentication is required.");
    }
}