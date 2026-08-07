namespace InterviewSimulator.Api.Features.Interviews;

public static class InterviewContractMapping
{
    public static InterviewType ToDomain(this InterviewTypeContract value) => value switch
    {
        InterviewTypeContract.Technical => InterviewType.Technical,
        InterviewTypeContract.Behavioral => InterviewType.Behavioral,
        InterviewTypeContract.SystemDesign => InterviewType.SystemDesign,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped interview type contract value."),
    };

    public static InterviewTypeContract ToContract(this InterviewType value) => value switch
    {
        InterviewType.Technical => InterviewTypeContract.Technical,
        InterviewType.Behavioral => InterviewTypeContract.Behavioral,
        InterviewType.SystemDesign => InterviewTypeContract.SystemDesign,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped interview type domain value."),
    };

    public static SeniorityLevel ToDomain(this SeniorityLevelContract value) => value switch
    {
        SeniorityLevelContract.Junior => SeniorityLevel.Junior,
        SeniorityLevelContract.Middle => SeniorityLevel.Middle,
        SeniorityLevelContract.Senior => SeniorityLevel.Senior,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped seniority level contract value."),
    };

    public static SeniorityLevelContract ToContract(this SeniorityLevel value) => value switch
    {
        SeniorityLevel.Junior => SeniorityLevelContract.Junior,
        SeniorityLevel.Middle => SeniorityLevelContract.Middle,
        SeniorityLevel.Senior => SeniorityLevelContract.Senior,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped seniority level domain value."),
    };

    public static InterviewStatusContract ToContract(this InterviewStatus value) => value switch
    {
        InterviewStatus.Created => InterviewStatusContract.Created,
        InterviewStatus.Active => InterviewStatusContract.Active,
        InterviewStatus.Completed => InterviewStatusContract.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped interview status domain value."),
    };

    public static InterviewStatus ToDomain(this InterviewStatusContract value) => value switch
    {
        InterviewStatusContract.Created => InterviewStatus.Created,
        InterviewStatusContract.Active => InterviewStatus.Active,
        InterviewStatusContract.Completed => InterviewStatus.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped interview status contract value."),
    };
}