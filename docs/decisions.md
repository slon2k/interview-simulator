# Architecture Decision Records

This document records significant architectural decisions for the AI Interview Simulator.

Each decision captures context, selected option, consequences, and alternatives considered.

---

## Decision Index

| ADR      | Title                                              | Status   |
| -------- | -------------------------------------------------- | -------- |
| ADR 0001 | Vite React + ASP.NET Core + App Service            | Accepted |
| ADR 0002 | Cosmos DB for Primary Persistence                  | Accepted |
| ADR 0003 | GitHub OAuth as Initial Identity Provider          | Accepted |
| ADR 0004 | Cookie Session Auth in ASP.NET Core                | Accepted |
| ADR 0005 | Session-Centric API Resource Model                 | Accepted |
| ADR 0006 | Deterministic Cosmos IDs and Partitioning Strategy | Accepted |
| ADR 0007 | IaC and CI/CD Baseline from Phase 1                | Accepted |
| ADR 0008 | Config-Based Invite Allowlist for MVP              | Accepted |
| ADR 0009 | Error Handling and ProblemDetails Standardization  | Accepted |

---

## ADR 0001: Vite React + ASP.NET Core + App Service

## Status

Accepted

## Context

The project is a personal, invite-only AI interview simulator focused on realistic practice, Azure integration experience, and low operational cost.

The detailed architecture is documented in [docs/architecture.md](architecture.md).

## Decision

Use an Azure-native stack with these core building blocks:

- Vite + React + TypeScript frontend
- Azure App Service for hosting
- ASP.NET Core API backend
- GitHub OAuth as the initial identity provider, with Microsoft Entra ID as a later option
- Invite-only access enforced through ASP.NET Core authorization policy and session cookies
- Azure Cosmos DB for session and turn persistence
- Azure OpenAI for question generation, evaluation, and summaries
- Azure AI Speech for optional speech-to-text and text-to-speech
- Application Insights for monitoring and diagnostics

## Consequences

### Benefits

- Low operational overhead
- Good fit for a solo, invite-only project
- Keeps the frontend, backend, AI, and storage stack Azure-native
- Supports a text-first MVP with optional voice later
- Standard ASP.NET Core auth and authorization model with full backend control

### Trade-offs

- App Service has more always-on infrastructure footprint than a fully serverless setup
- React and .NET may require duplicated DTOs unless shared contracts are added later
- Voice UX depends on browser-side Azure Speech SDK and temporary tokens

## Alternatives Considered

- ASP.NET Core Identity with local email/password accounts
- Blazor WebAssembly
- Blazor Server
- Azure Static Web Apps with Azure Functions

---

## ADR 0002: Cosmos DB for Primary Persistence

## Status

Accepted

## Context

The application stores user-scoped interview sessions, turns, summaries, and lightweight profile preferences. The expected MVP workload is invite-only, low to moderate traffic, and primarily document-shaped reads and writes.

## Decision

Use Azure Cosmos DB for NoSQL as the primary database for phase 1 and phase 2.

Use one container with typed documents for `session`, `turn`, and optional `profile`, partitioned by `userId`.

## Consequences

### Benefits

- Natural fit for session and turn documents
- Simple horizontal scaling model with partitioning
- Low operational overhead for MVP
- Good alignment with user-scoped access patterns

### Trade-offs

- Cross-document joins are limited compared to relational databases
- Query design must be intentional to avoid expensive scans
- Relational reporting workloads may require denormalization or export later

## Alternatives Considered

- Azure SQL Database
- PostgreSQL on Azure

---

## ADR 0003: GitHub OAuth as Initial Identity Provider

## Status

Accepted

## Context

The project is initially invite-only and developer-facing, with fast delivery prioritized over enterprise identity features.

## Decision

Use GitHub OAuth as the initial sign-in provider in ASP.NET Core.

Treat Microsoft Entra ID as an optional future provider.

## Consequences

### Benefits

- Fastest path to usable login for MVP
- Good fit for developer-oriented users
- Keeps auth flow simple in phase 1

### Trade-offs

- Less enterprise governance compared to Entra ID
- Org or team based restrictions can require extra GitHub API checks

## Alternatives Considered

- Microsoft Entra ID as the initial provider
- Supporting GitHub and Entra ID from day 1

---

## ADR 0004: Cookie Session Auth in ASP.NET Core

## Status

Accepted

## Context

The frontend and backend are served from the same application boundary on Azure App Service. The project requires invite-only access and secure API enforcement.

## Decision

Use ASP.NET Core authentication with secure HTTP-only cookies for authenticated sessions.

Enforce invite-only authorization in backend policies for protected API routes.

Apply anti-forgery protections for state-changing, cookie-authenticated endpoints.

## Consequences

### Benefits

- No browser token storage requirement
- Clear backend security boundary
- Good fit for same-origin deployment

### Trade-offs

- Requires careful cookie and CSRF configuration
- Cross-origin hosting becomes more complex if introduced later

## Alternatives Considered

- Token-only SPA auth in browser storage
- Managed platform auth as primary security boundary

---

## ADR 0005: Session-Centric API Resource Model

## Status

Accepted

## Context

Earlier planning mixed interview and session route naming. Consistent API naming is needed before implementation.

## Decision

Standardize on session-centric routes:

- `POST /api/sessions`
- `POST /api/sessions/{sessionId}/turns`
- `POST /api/sessions/{sessionId}/complete`
- `GET /api/sessions`
- `GET /api/sessions/{sessionId}`

## Consequences

### Benefits

- Clear domain language across frontend and backend
- Reduced route churn during implementation
- Easier API documentation and test coverage

### Trade-offs

- The session model can feel less intuitive for UI flows described as interviews, so naming discipline is required to keep endpoint design consistent over time

## Alternatives Considered

- Interview-centric route naming
- Mixed naming across endpoints

---

## ADR 0006: Deterministic Cosmos IDs and Partitioning Strategy

## Status

Accepted

## Context

The app must support user-scoped reads safely and efficiently while keeping query patterns simple. Document IDs must be derivable from domain keys without random generation.

## Decision

Use deterministic document IDs and user partitioning:

- **Session ID**: GUID (for domain/API use), stored in `sessionId` field using `"D"` format (with dashes, e.g. `550e8400-e29b-41d4-a716-446655440000`)
- **Session Cosmos ID**: `session|{guid}` (same format, e.g. `session|550e8400-e29b-41d4-a716-446655440000`)
- **Turn Cosmos ID**: `turn|{guid}|{turnNumber:D3}` with **zero-padded 3-digit turn number** for correct alphabetic ordering
  - Turn 1 → `001`, Turn 99 → `099`, Turn 100 → `100`, Turn 999 → `999` (orders correctly in string comparisons)
- **User Cosmos ID**: `{userId}` (format: `github|{providerId}`)
- **Partition key**: `/userId`

Use static `ToCosmosId()` methods on document classes for consistent ID derivation. Factory methods (`Create()`) provide validated construction.

Always read session and turn documents with the authenticated user partition key.

## Consequences

### Benefits

- Predictable lookups
- Stronger guardrails for user-scoped access
- Simpler debugging and diagnostics

### Trade-offs

- ID formats must be consistently enforced in code
- Migration would be needed if format changes later

## Alternatives Considered

- Random GUID IDs with secondary query patterns
- Multiple containers separated by document type

---

## ADR 0007: IaC and CI/CD Baseline from Phase 1

## Status

Accepted

## Context

The project should be reproducible and deployable from source control early, without relying on manual portal setup for repeatable infrastructure.

Some external setup, such as GitHub OAuth app registration and Azure OpenAI access/model availability, may still require manual validation or one-time configuration.

## Decision

Adopt infrastructure as code and CI/CD from phase 1:

- Bicep templates in the `infra/` folder for core resources
- GitHub Actions CI for build and test checks
- GitHub Actions CD for infrastructure and application deployment

## Consequences

### Benefits

- Reproducible environments
- Reduced configuration drift
- Higher deployment confidence from early automation

### Trade-offs

- Slightly higher setup effort in foundation phase
- Pipeline and template maintenance overhead

## Alternatives Considered

- Introduce IaC and CI/CD after MVP
- Partial automation with manual infrastructure setup

---

## ADR 0008: Config-Based Invite Allowlist for MVP

## Status

Accepted

## Context

The application is invite-only, but a dedicated admin UI for invite management is out of scope for the initial version.

The MVP needs a simple, low-ceremony way to control access without introducing invitation management infrastructure.

## Decision

Use a configuration-based allowlist of canonical application user IDs for MVP access control.

Use separate configuration values for admin users and invited users:

```text
AccessControl:AdminUserIds
AccessControl:InvitedUserIds
```

Treat admin users as invited users for access control.

Use the GitHub numeric user ID as the stable external identifier and normalize internal identity as:

```text
github|{githubUserId}
```

## Consequences

### Benefits

- Very simple to implement and operate for MVP
- No admin management UI required
- Access changes can be made through configuration
- Clear distinction between admin and invited users without extra infrastructure

### Trade-offs

- Updating invite lists requires configuration changes
- No self-service invite management in phase 1
- Allowlist maintenance is manual until a later admin tool exists

## Alternatives Considered

- Cosmos-backed invite registry
- GitHub org or team membership checks
- Admin UI for invite management

---

## ADR 0009: Error Handling and ProblemDetails Standardization

## Status

Accepted

## Context

Error responses had started diverging across endpoint-local result payloads, validation outputs, and unhandled exception paths.

Domain exception primitives were introduced, but without a centralized exception mapping path the API could still return inconsistent error shapes and status behavior.

The project needs a stable machine-readable error contract before additional feature delivery to avoid client-facing drift.

## Decision

Use RFC 7807 ProblemDetails as the canonical transport envelope for all API 4xx and 5xx responses.

Keep domain exceptions in domain logic and map them at the API boundary only.

Translate infrastructure-specific failures, such as Cosmos DB SDK errors, at the repository boundary into stable infrastructure exception types before they reach the API pipeline.

Standardize API error metadata through ProblemDetails extensions:

- `code`: stable machine-readable error code
- `traceId`: request correlation identifier
- `details`: validation detail entries when applicable

Add a centralized exception mapping path in the ASP.NET Core pipeline for DomainException and InfrastructureException types.

## Exception to Status Mapping

- `DomainRuleViolationException` -> `400 Bad Request` and `ErrorType.Validation`
- `DomainConflictException` -> `409 Conflict` and `ErrorType.Conflict`
- `DomainNotFoundException` -> `404 Not Found` and `ErrorType.NotFound`
- Repository-translated infrastructure concurrency failures -> `409 Conflict` and `ErrorType.Concurrency`
- Repository-translated infrastructure transient dependency failures -> `503 Service Unavailable` and `ErrorType.Unavailable`
- Authentication failures -> `401 Unauthorized` and `ErrorType.Unauthorized`
- Authorization failures -> `403 Forbidden` and `ErrorType.Forbidden`
- Concurrency conflicts -> `409 Conflict` and `ErrorType.Concurrency`
- Rate limit breaches -> `429 Too Many Requests` and `ErrorType.RateLimit`
- Upstream dependency unavailable -> `503 Service Unavailable` and `ErrorType.Unavailable`
- Unmapped exceptions -> `500 Internal Server Error` and `ErrorType.Unexpected`

## Error Code Conventions

Use stable dotted namespace-style codes:

```text
Feature.Aggregate.RuleName
```

Codes are part of the public API contract and must remain stable after release.

Message text may evolve for clarity, but code values should not be renamed.

## Logging Severity Guidance

Use these default levels at the API boundary:

- `Information`: expected domain exceptions caused by normal user behavior (validation failures, common business-rule conflicts, and not found outcomes from stale client state or user input)
- `Warning`: domain exceptions that are unusual in normal flows and may indicate misuse, abuse, or client defects
- `Error`: operational failures (infrastructure exceptions, unhandled exceptions, dependency outages)

## Scope

Included:

- ASP.NET Core API domain and endpoint error mapping in the current backend project
- Repository-level translation of infrastructure SDK failures into API-safe exception types
- Validation and business-rule failure standardization

Excluded:

- Frontend localization policy for server-provided error messages
- Cross-service distributed error taxonomy outside this API boundary

## Migration Strategy

1. Add centralized DomainException and InfrastructureException to ProblemDetails mapping in the diagnostics pipeline.
2. Replace endpoint ad hoc error payloads with shared mapping output.
3. Align remaining business-rule InvalidOperationException usage to DomainException taxonomy where appropriate.
4. Translate repository infrastructure failures into stable infrastructure exceptions.
5. Add tests for mapping correctness and payload shape consistency.

## Consequences

### Benefits

- Consistent client-visible error contract
- Better operability through stable codes and trace correlation
- Reduced per-endpoint duplication and decision churn

### Trade-offs

- Migration touches handlers and related tests
- Requires discipline to keep domain exceptions transport-agnostic

## Alternatives Considered

- Keep endpoint-local ad hoc payload conventions
- Use a custom error envelope instead of ProblemDetails
- Introduce a shared-kernel project now for all error contracts
