using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.Profile;
using InterviewSimulator.Api.Infrastructure.Identity;

namespace InterviewSimulator.Api.UnitTests.Infrastructure.Identity;

public sealed class UserDocumentMapper_CreateOrUpdate
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewUser_CreatesDocumentWithGuestAccessLevel()
    {
        var document = UserDocumentMapper.CreateOrUpdate(existing: null, BuildProfile("github|100"), Now);

        Assert.Equal(UserAccessLevels.Guest, document.AccessLevel);
    }

    [Fact]
    public void NewUser_SetsAllIdentityFields()
    {
        var profile = BuildProfile(
            "github|100",
            login: "octocat",
            displayName: "Octocat",
            avatarUrl: "https://example.com/avatar.png");

        var document = UserDocumentMapper.CreateOrUpdate(existing: null, profile, Now);

        Assert.Equal("github|100", document.Id);
        Assert.Equal("github|100", document.UserId);
        Assert.Equal("user", document.Type);
        Assert.Equal(1, document.SchemaVersion);
        Assert.Equal("github", document.Provider);
        Assert.Equal("100", document.ProviderUserId);
        Assert.Equal("octocat", document.GithubLogin);
        Assert.Equal("Octocat", document.DisplayName);
        Assert.Equal("https://example.com/avatar.png", document.AvatarUrl);
        Assert.False(document.IsDisabled);
    }

    [Fact]
    public void NewUser_SetsAllTimestampsToNow()
    {
        var document = UserDocumentMapper.CreateOrUpdate(existing: null, BuildProfile("github|100"), Now);

        Assert.Equal(Now, document.FirstSeenAt);
        Assert.Equal(Now, document.LastSeenAt);
        Assert.Equal(Now, document.CreatedAt);
        Assert.Equal(Now, document.UpdatedAt);
    }

    [Fact]
    public void ExistingUser_DoesNotOverwriteAccessLevel()
    {
        var existing = BuildDocument("github|100", accessLevel: UserAccessLevels.Member);

        var document = UserDocumentMapper.CreateOrUpdate(existing, BuildProfile("github|100"), Now);

        Assert.Equal(UserAccessLevels.Member, document.AccessLevel);
    }

    [Fact]
    public void ExistingUser_DoesNotOverwriteFirstSeenAt()
    {
        var originalFirstSeen = Now.AddDays(-30);
        var existing = BuildDocument("github|100", firstSeenAt: originalFirstSeen);

        var document = UserDocumentMapper.CreateOrUpdate(existing, BuildProfile("github|100"), Now);

        Assert.Equal(originalFirstSeen, document.FirstSeenAt);
    }

    [Fact]
    public void ExistingUser_UpdatesProfileFields()
    {
        var existing = BuildDocument("github|100", login: "old-login", displayName: "Old Name");
        var profile = BuildProfile(
            "github|100",
            login: "new-login",
            displayName: "New Name",
            avatarUrl: "https://example.com/new.png");

        var document = UserDocumentMapper.CreateOrUpdate(existing, profile, Now);

        Assert.Equal("new-login", document.GithubLogin);
        Assert.Equal("New Name", document.DisplayName);
        Assert.Equal("https://example.com/new.png", document.AvatarUrl);
    }

    [Fact]
    public void ExistingUser_UpdatesLastSeenAtAndUpdatedAt()
    {
        var existing = BuildDocument("github|100", lastSeenAt: Now.AddDays(-1));

        var document = UserDocumentMapper.CreateOrUpdate(existing, BuildProfile("github|100"), Now);

        Assert.Equal(Now, document.LastSeenAt);
        Assert.Equal(Now, document.UpdatedAt);
    }

    [Fact]
    public void ExistingUser_ReturnsSameInstance()
    {
        var existing = BuildDocument("github|100");

        var document = UserDocumentMapper.CreateOrUpdate(existing, BuildProfile("github|100"), Now);

        Assert.Same(existing, document);
    }

    private static AuthenticatedUserProfile BuildProfile(
        string userId,
        string login = "test-user",
        string? displayName = null,
        string? avatarUrl = null)
        => new(
            UserId: userId,
            Provider: "github",
            ProviderUserId: userId["github|".Length..],
            GithubLogin: login,
            DisplayName: displayName,
            AvatarUrl: avatarUrl);

    private static CosmosUserDocument BuildDocument(
        string userId,
        string accessLevel = UserAccessLevels.Guest,
        string login = "test-user",
        string? displayName = null,
        bool isDisabled = false,
        DateTimeOffset? firstSeenAt = null,
        DateTimeOffset? lastSeenAt = null)
        => new()
        {
            Id = userId,
            UserId = userId,
            Provider = "github",
            ProviderUserId = userId["github|".Length..],
            GithubLogin = login,
            DisplayName = displayName,
            AccessLevel = accessLevel,
            IsDisabled = isDisabled,
            FirstSeenAt = firstSeenAt ?? Now.AddDays(-7),
            LastSeenAt = lastSeenAt ?? Now.AddHours(-1),
            CreatedAt = Now.AddDays(-7),
            UpdatedAt = Now.AddHours(-1),
        };
}

public sealed class UserDocumentMapper_ToAccessSnapshot
{
    [Fact]
    public void MapsUserIdAccessLevelAndDisabledFlag()
    {
        var document = new CosmosUserDocument
        {
            Id = "github|100",
            UserId = "github|100",
            AccessLevel = UserAccessLevels.Admin,
            IsDisabled = false,
        };

        var snapshot = UserDocumentMapper.ToAccessSnapshot(document);

        Assert.Equal("github|100", snapshot.UserId);
        Assert.Equal(UserAccessLevels.Admin, snapshot.AccessLevel);
        Assert.False(snapshot.IsDisabled);
    }

    [Fact]
    public void DisabledUser_MapsIsDisabledTrue()
    {
        var document = new CosmosUserDocument
        {
            Id = "github|100",
            UserId = "github|100",
            AccessLevel = UserAccessLevels.Member,
            IsDisabled = true,
        };

        var snapshot = UserDocumentMapper.ToAccessSnapshot(document);

        Assert.True(snapshot.IsDisabled);
    }
}
