# Development Guide

This guide covers local development concerns that should stay separate from Azure deployment setup.

## Cosmos DB Emulator (Docker)

For local development, run the Cosmos DB emulator using Docker Compose:

```powershell
docker compose up cosmos-emulator -d
```

The emulator starts with data persistence enabled. First-time setup requires trusting the emulator's self-signed certificate:

```powershell
# Wait for the emulator to finish starting (11/11 partitions), then:
curl.exe -k https://localhost:8081/_explorer/emulator.pem -o "$env:TEMP\cosmos-emulator.pem"
Import-Certificate -FilePath "$env:TEMP\cosmos-emulator.pem" -CertStoreLocation "Cert:\CurrentUser\Root"
```

Re-import the certificate if you recreate the container with `docker compose down -v` (volume deletion regenerates the cert).

The emulator connection string is pre-configured in `appsettings.Development.json`. `InitializeOnStartup: true` auto-creates the database and containers on first run.

## Local Speech Configuration

The API validates Azure Speech configuration on startup.

Required configuration keys:

- `AzureSpeech:Region`
- `AzureSpeech:Endpoint`
- `AzureSpeech:TokenEndpoint`
- `AzureSpeech:Key`

The expected configuration shape is visible in:

- [api/src/InterviewSimulator.Api/appsettings.json](../api/src/InterviewSimulator.Api/appsettings.json)
- [api/src/InterviewSimulator.Api/appsettings.Development.json](../api/src/InterviewSimulator.Api/appsettings.Development.json)

For local development, prefer `dotnet user-secrets` for the Speech key and any other sensitive values.

Example setup:

```powershell
dotnet user-secrets set "AzureSpeech:Region" "centralus" --project "api/src/InterviewSimulator.Api/InterviewSimulator.Api.csproj"
dotnet user-secrets set "AzureSpeech:Endpoint" "https://<speech-account>.cognitiveservices.azure.com/" --project "api/src/InterviewSimulator.Api/InterviewSimulator.Api.csproj"
dotnet user-secrets set "AzureSpeech:TokenEndpoint" "https://centralus.api.cognitive.microsoft.com/sts/v1.0/issueToken" --project "api/src/InterviewSimulator.Api/InterviewSimulator.Api.csproj"
dotnet user-secrets set "AzureSpeech:Key" "<speech-key>" --project "api/src/InterviewSimulator.Api/InterviewSimulator.Api.csproj"
```

Notes:

- Do not commit real Speech keys to `appsettings*.json`.
- Startup validation is intentionally fail-fast when required values are missing.
- Validation errors must not log secret values.

## Azure Runtime Configuration

In Azure App Service, the non-secret Azure Speech settings are injected through app settings.

`AzureSpeech__Key` is supplied through an App Service Key Vault reference rather than a plain-text setting.

See [docs/deployment.md](./deployment.md) for deployment workflow, Azure RBAC, and Key Vault setup.

## User Persistence on OAuth Login

**Status**: Implemented in milestone 03, out-of-scope but essential for OAuth integration.

When a user authenticates via GitHub OAuth, `UserProfilePersistenceMiddleware` automatically persists the user profile to Cosmos DB:

1. Middleware runs after authentication, captures `AuthenticatedUserProfile` from claims
2. Calls `IUserProfileStore.UpsertAuthenticatedUserProfileAsync()` to save/update the user
3. Gracefully handles errors (logs warning, allows request to proceed)
4. On new login: creates user document with `AccessLevel=Guest` by default
5. On subsequent logins: updates `LastSeenAt` and profile fields (GitHub login, display name, avatar)

This enables:

- Access control to recognize authenticated users
- User session tracking (first seen, last seen timestamps)
- Foundation for invite allowlist and admin override

**Implementation details**:

- `UserProfilePersistenceMiddleware` in `Infrastructure/Identity/`
- `CosmosIdentityUserStore` persists to Cosmos; `DisabledIdentityUserStore` is a no-op when Cosmos disabled
- Tests use `TestUserProfileStore` with pre-seeded member/admin users
- Partition key: `/userId` (format: `github|{githubId}`)

**Performance note**: Currently writes on every authenticated request. This is acceptable for MVP but debouncing (e.g., update max once per 5 minutes) should be considered before large-scale deployment. Monitor Cosmos DB RU consumption and implement debouncing if needed.

## Session and Turn Persistence

**Status**: Fully implemented in milestones 03 and 04.

### Document Models

Session and turn documents follow the deterministic ID strategy documented in ADR 0006:

- Session ID format: `session|{guid:D}`
- Turn ID format: `turn|{guid:D}|{turnNumber:D3}` (D3 padding for correct alphabetic ordering)

### Creating and reading via CosmosInterviewStore

```csharp
// Create a session
var session = InterviewSession.Create(
    userId: userId,
    targetRole: "Backend Engineer",
    focusArea: "dotnet",
    seniority: SeniorityLevel.Senior,
    interviewType: InterviewType.Technical,
    questionCount: 5,
    createdAt: DateTimeOffset.UtcNow);

await store.CreateSessionAsync(session, cancellationToken);

// Read a session (ConcurrencyToken is populated from _etag via ItemResponse.ETag)
var session = await store.GetSessionAsync(userId, sessionId, cancellationToken);

// Read a turn
var turn = await store.GetTurnAsync(userId, sessionId, turnNumber, cancellationToken);
```

All mutable write operations (`StartInterviewAsync`, `SaveAnswerAsync`, `UpdateSessionAsync`) require a non-empty `ConcurrencyToken` on the session/turn. Always read before writing.

Static `ToCosmosId()` methods ensure consistent ID derivation across CRUD operations. Always pass the authenticated `userId` as the partition key.
