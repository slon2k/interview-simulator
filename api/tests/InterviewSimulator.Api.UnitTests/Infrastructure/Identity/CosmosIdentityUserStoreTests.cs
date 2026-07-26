using InterviewSimulator.Api.Features.Identity.Access;
using InterviewSimulator.Api.Features.Identity.CurrentUser;
using InterviewSimulator.Api.Infrastructure.Data;
using InterviewSimulator.Api.Infrastructure.Identity;

namespace InterviewSimulator.Api.UnitTests.Infrastructure.Identity;

public sealed class CosmosIdentityUserStore_UpsertAuthenticatedUserProfile
{
    [Fact]
    public async Task NewUser_CreatesDocumentWithGuestAccessLevel()
    {
        var repo = new InMemoryUserRepository();
        var store = new CosmosIdentityUserStore(repo);
        var profile = BuildProfile("github|100");

        await store.UpsertAuthenticatedUserProfileAsync(profile);

        var saved = repo.GetStored("github|100");
        Assert.NotNull(saved);
        Assert.Equal(UserAccessLevels.Guest, saved.AccessLevel);
    }

    [Fact]
    public async Task NewUser_SetsAllIdentityFields()
    {
        var repo = new InMemoryUserRepository();
        var store = new CosmosIdentityUserStore(repo);
        var profile = BuildProfile("github|100", login: "octocat", displayName: "Octocat", avatarUrl: "https://example.com/avatar.png");

        await store.UpsertAuthenticatedUserProfileAsync(profile);

        var saved = repo.GetStored("github|100");
        Assert.NotNull(saved);
        Assert.Equal("github|100", saved.Id);
        Assert.Equal("github|100", saved.UserId);
        Assert.Equal("user", saved.Type);
        Assert.Equal(1, saved.SchemaVersion);
        Assert.Equal("github", saved.Provider);
        Assert.Equal("100", saved.ProviderUserId);
        Assert.Equal("octocat", saved.GithubLogin);
        Assert.Equal("Octocat", saved.DisplayName);
        Assert.Equal("https://example.com/avatar.png", saved.AvatarUrl);
        Assert.False(saved.IsDisabled);
    }

    [Fact]
    public async Task NewUser_SetsFirstSeenAtAndLastSeenAt()
    {
        var repo = new InMemoryUserRepository();
        var store = new CosmosIdentityUserStore(repo);
        var profile = BuildProfile("github|100");

        var before = DateTimeOffset.UtcNow;
        await store.UpsertAuthenticatedUserProfileAsync(profile);
        var after = DateTimeOffset.UtcNow;

        var saved = repo.GetStored("github|100");
        Assert.NotNull(saved);
        Assert.InRange(saved.FirstSeenAt, before, after);
        Assert.InRange(saved.LastSeenAt, before, after);
        Assert.InRange(saved.CreatedAt, before, after);
        Assert.InRange(saved.UpdatedAt, before, after);
    }

    [Fact]
    public async Task ExistingUser_DoesNotOverwriteAccessLevel()
    {
        var repo = new InMemoryUserRepository();
        repo.Seed("github|100", accessLevel: UserAccessLevels.Member);
        var store = new CosmosIdentityUserStore(repo);
        var profile = BuildProfile("github|100");

        await store.UpsertAuthenticatedUserProfileAsync(profile);

        var saved = repo.GetStored("github|100");
        Assert.NotNull(saved);
        Assert.Equal(UserAccessLevels.Member, saved.AccessLevel);
    }

    [Fact]
    public async Task ExistingUser_DoesNotOverwriteFirstSeenAt()
    {
        var originalFirstSeen = DateTimeOffset.UtcNow.AddDays(-30);
        var repo = new InMemoryUserRepository();
        repo.Seed("github|100", firstSeenAt: originalFirstSeen);
        var store = new CosmosIdentityUserStore(repo);
        var profile = BuildProfile("github|100");

        await store.UpsertAuthenticatedUserProfileAsync(profile);

        var saved = repo.GetStored("github|100");
        Assert.NotNull(saved);
        Assert.Equal(originalFirstSeen, saved.FirstSeenAt);
    }

    [Fact]
    public async Task ExistingUser_UpdatesProfileFields()
    {
        var repo = new InMemoryUserRepository();
        repo.Seed("github|100", login: "old-login", displayName: "Old Name");
        var store = new CosmosIdentityUserStore(repo);
        var profile = BuildProfile("github|100", login: "new-login", displayName: "New Name", avatarUrl: "https://example.com/new.png");

        await store.UpsertAuthenticatedUserProfileAsync(profile);

        var saved = repo.GetStored("github|100");
        Assert.NotNull(saved);
        Assert.Equal("new-login", saved.GithubLogin);
        Assert.Equal("New Name", saved.DisplayName);
        Assert.Equal("https://example.com/new.png", saved.AvatarUrl);
    }

    [Fact]
    public async Task ExistingUser_UpdatesLastSeenAt()
    {
        var repo = new InMemoryUserRepository();
        repo.Seed("github|100", lastSeenAt: DateTimeOffset.UtcNow.AddDays(-1));
        var store = new CosmosIdentityUserStore(repo);
        var profile = BuildProfile("github|100");

        var before = DateTimeOffset.UtcNow;
        await store.UpsertAuthenticatedUserProfileAsync(profile);
        var after = DateTimeOffset.UtcNow;

        var saved = repo.GetStored("github|100");
        Assert.NotNull(saved);
        Assert.InRange(saved.LastSeenAt, before, after);
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
}

public sealed class CosmosIdentityUserStore_GetAccessByUserId
{
    [Fact]
    public async Task KnownUser_ReturnsAccessSnapshot()
    {
        var repo = new InMemoryUserRepository();
        repo.Seed("github|100", accessLevel: UserAccessLevels.Admin, isDisabled: false);
        var store = new CosmosIdentityUserStore(repo);

        var result = await store.GetAccessByUserIdAsync("github|100");

        Assert.NotNull(result);
        Assert.Equal("github|100", result.UserId);
        Assert.Equal(UserAccessLevels.Admin, result.AccessLevel);
        Assert.False(result.IsDisabled);
    }

    [Fact]
    public async Task DisabledUser_ReturnsSnapshotWithIsDisabledTrue()
    {
        var repo = new InMemoryUserRepository();
        repo.Seed("github|100", accessLevel: UserAccessLevels.Member, isDisabled: true);
        var store = new CosmosIdentityUserStore(repo);

        var result = await store.GetAccessByUserIdAsync("github|100");

        Assert.NotNull(result);
        Assert.True(result.IsDisabled);
    }

    [Fact]
    public async Task UnknownUser_ReturnsNull()
    {
        var repo = new InMemoryUserRepository();
        var store = new CosmosIdentityUserStore(repo);

        var result = await store.GetAccessByUserIdAsync("github|999");

        Assert.Null(result);
    }
}

/// <summary>
/// In-memory repository stub for unit testing CosmosIdentityUserStore.
/// </summary>
file sealed class InMemoryUserRepository : IRepository<CosmosUserDocument>
{
    private readonly Dictionary<string, CosmosUserDocument> _store = [];

    public void Seed(
        string userId,
        string accessLevel = UserAccessLevels.Guest,
        string login = "test-user",
        string? displayName = null,
        bool isDisabled = false,
        DateTimeOffset? firstSeenAt = null,
        DateTimeOffset? lastSeenAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        _store[userId] = new CosmosUserDocument
        {
            Id = userId,
            UserId = userId,
            Provider = "github",
            ProviderUserId = userId["github|".Length..],
            GithubLogin = login,
            DisplayName = displayName,
            AccessLevel = accessLevel,
            IsDisabled = isDisabled,
            FirstSeenAt = firstSeenAt ?? now.AddDays(-7),
            LastSeenAt = lastSeenAt ?? now.AddHours(-1),
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddHours(-1),
        };
    }

    public CosmosUserDocument? GetStored(string userId)
        => _store.GetValueOrDefault(userId);

    public Task<CosmosUserDocument?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task<CosmosUserDocument> UpsertAsync(CosmosUserDocument document, string partitionKey, CancellationToken cancellationToken = default)
    {
        _store[document.Id] = document;
        return Task.FromResult(document);
    }

    public Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        _store.Remove(id);
        return Task.CompletedTask;
    }
}
