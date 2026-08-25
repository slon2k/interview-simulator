using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Dashboard;

public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    public int RecentSessionsLimit { get; set; } = 5;

    public int ScoreTrendLimit { get; init; } = 5;
}

public sealed class DashboardOptionsValidator : IValidateOptions<DashboardOptions>
{
    public ValidateOptionsResult Validate(string? name, DashboardOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.RecentSessionsLimit <= 0)
        {
            failures.Add("Dashboard:RecentSessionsLimit must be greater than 0.");
        }

        if (options.ScoreTrendLimit <= 0)
        {
            failures.Add("Dashboard:ScoreTrendLimit must be greater than 0.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}