using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

namespace InterviewSimulator.Api.Features.Auth;

public sealed class AccessControlOptionsValidator : IValidateOptions<AccessControlOptions>
{
    private static readonly Regex _canonicalUserIdRegex = new(
        @"^github\|[1-9][0-9]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ValidateOptionsResult Validate(string? name, AccessControlOptions options)
    {
        var failures = new List<string>();

        ValidateUserIds(
            options.InvitedUserIds,
            nameof(AccessControlOptions.InvitedUserIds),
            failures);

        ValidateUserIds(
            options.AdminUserIds,
            nameof(AccessControlOptions.AdminUserIds),
            failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateUserIds(
        IReadOnlyList<string>? userIds,
        string propertyName,
        List<string> failures)
    {
        if (userIds is null)
        {
            return;
        }

        var seenUserIds = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < userIds.Count; index++)
        {
            var userId = userIds[index];
            var configPath = $"{AccessControlOptions.SectionName}:{propertyName}:{index}";

            if (string.IsNullOrWhiteSpace(userId))
            {
                failures.Add($"{configPath} must not be empty.");
                continue;
            }

            if (userId != userId.Trim())
            {
                failures.Add($"{configPath} must not contain leading or trailing whitespace.");
                continue;
            }

            if (!_canonicalUserIdRegex.IsMatch(userId))
            {
                failures.Add(
                    $"{configPath} must use canonical user ID format: github|<numeric-github-id>. Actual value: {userId}");
                continue;
            }

            if (!seenUserIds.Add(userId))
            {
                failures.Add($"{configPath} contains duplicate user ID: {userId}");
            }
        }
    }
}