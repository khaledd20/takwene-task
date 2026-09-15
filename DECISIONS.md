# Engineering Decisions & Technical Reflection

This document details the architectural choices, security remediations, and debugging workflows undertaken during the development of the Takwene Track Management System, explicitly contrasting the **Agent's automated generation** with the **User's technical oversight, architectural direction, and manual interventions**.

---

## 1. Architectural Collaboration: Agent Scaffolding vs. User Engineering

The Agent was utilized for boilerplate generation and syntax scaffolding, while the User maintained full architectural control, established software boundaries, and designed the domain state machine.

### Comparison Matrix

| Component / Subsystem | What the Agent Generated | What the User Directed, Designed & Refactored |
|---|---|---|
| **Solution & Project Layering** | Generated standard .NET 9 project templates (`dotnet new sln`, classlib, webapi). Initially attempted to install EF Core into all projects. | **User Intervention**: Enforced strict Clean Architecture boundaries. Stripped third-party ORM packages from `Takwene.Domain`, keeping it 100% pure. Defined the `IApplicationDbContext` contract in `Takwene.Application` and isolated all EF Core dependencies to `Takwene.Infrastructure`. |
| **Data Model & Schema Constraints** | Generated entity property declarations and DTOs with basic scalar types. Defaulted to storing enums as raw integers. | **User Intervention**: Designed relational integrity rules—added unique indexes on `Artist.Email` and `Track.Isrc`, configured composite unique index `(TrackId, DspId)` on `TrackDistribution`, and configured explicit `EnumToStringConverter` mappings so statuses are stored as readable strings. |
| **Domain State Machine** | Treated track and DSP distribution statuses as disconnected, static string fields. | **User Intervention**: Designed the business lifecycle state machine: dispatching a draft track automatically transitions its catalog status to `submitted` with `pending` distributions; marking a track `distributed` automatically cascades pending DSP distributions to `live`. |
| **API Error Handling** | Produced default 500 stack traces and unformatted exception messages. | **User Intervention**: Architected custom domain exceptions (`NotFoundException`, `ConflictException`, `DomainException`) and implemented centralized RFC 7807 `ProblemDetails` middleware mapping errors to proper HTTP status codes (400, 404, 409). |
| **Frontend Framework & Architecture** | Scaffolded raw, unoptimized boilerplate. Initially suggested React with experimental bundler tooling. | **User Intervention**: Architected the frontend in **Angular 19** using modern Standalone Components, TypeScript domain models matching backend DTOs, reactive signals (`signal`), and `provideHttpClient(withFetch())`. Eliminated legacy `NgModule` overhead and third-party UI framework bloat. |
| **Automated Testing & Verification** | Generated individual test file templates without ensuring solution-wide execution. | **User Intervention**: Mandated automated test execution as a strict quality gate before commits. Executed `dotnet test backend/Takwene.sln`, validating 10 passing unit tests across domain rules, state transitions, and SQLite in-memory relational integrity. |

---

## 2. Security Audit & Remediations

A security review of the Agent's initial code revealed several critical vulnerabilities that the User identified and instructed the Agent to remediate:

### Vulnerability 1: Unprotected State Mutations
- **Agent Generation**: Generated open controller endpoints without authentication filters. Any anonymous client could update track statuses, create artists, or trigger simulated DSP dispatches.
- **User Analysis & Fix**: 
  - User directed the addition of JWT Bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`).
  - Applied `[Authorize]` attributes to all state-modifying actions (`POST /api/tracks`, `POST /api/tracks/{id}/distribute`, `PATCH /api/tracks/{id}/status`).
  - Configured strict validation parameters in `Program.cs` (`ValidateIssuer = true`, `ValidateAudience = true`, `ValidateLifetime = true`, `ValidateIssuerSigningKey = true`, and `ClockSkew = TimeSpan.Zero` to eliminate token expiration leeway).
  - Pre-configured Swagger UI with Bearer Authentication so evaluators can authenticate with one click.

### Vulnerability 2: Over-Posting & Mass Assignment
- **Agent Generation**: Bound raw database entity classes directly to action methods in controllers (e.g. `CreateTrack([FromBody] Track track)`). This exposed internal entity fields (such as client-supplied IDs, creation dates, and status bypasses) to external manipulation.
- **User Analysis & Fix**:
  - User enforced strict decoupling of API contracts from persistence models by introducing dedicated Request DTOs (`CreateTrackRequest`, `UpdateTrackStatusRequest`, `DistributeTrackRequest`).
  - Introduced `FluentValidation` validators to enforce data invariants before payloads reach domain services.

### Vulnerability 3: Lack of Input Sanitization on Catalog Identifiers (ISRC)
- **Agent Generation**: Accepted arbitrary string inputs for ISRC codes, risking invalid catalog entries, malformed codes, and duplicate records caused by casing and punctuation variations.
- **User Analysis & Fix**:
  - User defined strict ISO 3901 validation using compiled regular expressions (`^[A-Z]{2}-?[A-Z0-9]{3}-?[0-9]{2}-?[0-9]{5}$`).
  - Implemented sanitization logic that strips hyphens and forces uppercase before uniqueness checks and persistence.

### Empirical Validation: Strong Type Safety & GUID Rejection
- **User Live Testing**: Tested `POST /api/tracks` via Swagger with a valid Bearer token and intentionally supplied a malformed string (`"artistId": "fdasfgsdf95191"`) instead of a valid GUID.
- **Observed Behavior**: The API immediately halted processing and returned an RFC 7807 `400 Bad Request` ProblemDetails payload:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
      "$.artistId": ["The JSON value could not be converted to System.Guid. Path: $.artistId | LineNumber: 2"]
    }
  }
  ```
- **Takeaway**: This confirmed that JWT authentication correctly authenticated the caller, while strict framework-level model binding and schema constraints successfully reject malformed primary/foreign keys before any database operations occur.

---

## 3. Critical Bug Diagnosis: EF Core Concurrency Failure

The most significant technical bug encountered during the project was an ORM state-tracking flaw introduced by the Agent.

### The Bug Behavior
When running unit tests for `DistributeTrackAsync`, the operation failed with:
```
Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
The database operation was expected to affect 1 row(s), but actually affected 0 row(s);
data may have been modified or deleted since entities were loaded.
```

### Agent's Implementation & Flawed Diagnosis
1. **Agent's Code**: In `TrackDistribution.cs`, the Agent initialized the primary key with a non-default value:
   ```csharp
   public class TrackDistribution
   {
       public Guid Id { get; set; } = Guid.NewGuid(); // <-- Pre-set client key
       ...
   }
   ```
2. In `TrackService.cs`, the Agent appended the new entity directly to the tracked track's navigation collection:
   ```csharp
   track.Distributions.Add(new TrackDistribution { Id = Guid.NewGuid(), ... });
   ```
3. **Agent's Flawed Suggestion**: When the concurrency exception occurred, the Agent recommended adding optimistic concurrency tokens (`RowVersion`) to the entity. The User recognized that this merely treated the symptom while misunderstanding the underlying ORM behavior.

### User's Root Cause Analysis & Fix
1. **Root Cause**: In Entity Framework Core, when an entity is added via a navigation property onto an already-tracked principal entity (`track`), EF Core inspects the primary key:
   - If `Id == Guid.Empty` (default), EF Core sets `EntityEntry.State = EntityState.Added` (`INSERT`).
   - Because `Id` was already populated (`Guid.NewGuid()`), EF Core's Change Tracker inferred that the entity was an *existing* record loaded from the database, incorrectly marking its state as **`EntityState.Modified`**!
2. During `SaveChangesAsync()`, EF Core generated:
   ```sql
   UPDATE "TrackDistributions" SET ... WHERE "Id" = @p4;
   ```
   Because `@p4` was a brand new GUID that never existed in SQLite, 0 rows were updated. EF Core interpreted 0 rows affected as an optimistic concurrency conflict and threw `DbUpdateConcurrencyException`.
3. **User's Fix**: The User enabled SQL command tracing (`LogTo(Console.WriteLine)`), identified the rogue `UPDATE`, and instructed the Agent to explicitly register the child entity through the `DbSet`:
   ```csharp
   // User-directed fix:
   var newDist = new TrackDistribution
   {
       Id = Guid.NewGuid(),
       TrackId = track.Id,
       DspId = dspId,
       SubmittedAt = DateTime.UtcNow,
       Status = DistributionStatus.Pending
   };
   _context.TrackDistributions.Add(newDist); // Explicitly forces EntityState.Added
   ```
   Calling `_context.TrackDistributions.Add()` forces the Change Tracker to mark the state as `EntityState.Added`, generating the correct `INSERT` statement and resolving the issue.

---

## 4. Quality Assurance & Test Verification: User-Driven Testing

To prevent regressions and ensure domain rules hold true before code reaches version control, the User enforced a strict quality gate: each tier must be empirically verified before committing.

### User Execution: Solution-Wide Unit Test Suite
The User executed the automated unit test suite across all projects in the solution:
```bash
dotnet test backend/Takwene.sln
```

### Verified Test Scenarios & Results
All 10 automated unit tests executed and passed cleanly:
```
Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10, Duration: 2 s - Takwene.UnitTests.dll (net9.0)
```

The User verified the following critical business cases:
1. **Catalog Drafting (`CreateTrackAsync`)**: Verifies tracks initialize in `draft` status with valid metadata and relational artist mapping.
2. **Relational & ISO 3901 Invariants**: Verifies duplicate ISRC codes immediately throw `ConflictException` (409) and invalid formats trigger `ValidationException` (400).
3. **State Machine Integrity**:
   - Dispatching a draft track automatically transitions its catalog status to `submitted` with `pending` distributions.
   - Dispatching an already submitted or distributed track prevents illegal status reversals.
4. **Cascading State Transitions**: Marking a track as `distributed` cascades all pending DSP distributions to `live` simultaneously.
5. **Artist Uniqueness & Constraints**: Verifies artist creation and duplicate email rejections.

**User vs. Agent Distinction**: While the Agent drafted the unit test file templates, the User defined the mandatory test checklist in `Demo.md`, executed `dotnet test`, analyzed the execution output, and verified that all 10 business rules passed before authorizing the commit to the repository.

---

## 5. Frontend Architecture & User Interventions: Modern Angular 19 SPA

For the front-end (Task 2), the User chose **Angular 19** over React to leverage strong TypeScript end-to-end type safety, modern standalone components, and native reactive signals, while directing the Agent to eliminate legacy module overhead.

### User Design & Architectural Edits
1. **Modern Standalone Architecture (Zero Legacy Overhead)**:
   - The User enforced modern Angular standalone architecture (`standalone: true`, `imports: [...]`), completely avoiding legacy `NgModule` boilerplate and heavy third-party UI libraries.
   - Configured `provideHttpClient(withFetch())` in `app.config.ts` for native, efficient HTTP streaming.
2. **Native Reactive Signals for Authentication State**:
   - Instead of pulling in external state managers (e.g. NgRx or Redux), the User implemented lightweight, fine-grained reactive state using Angular Signals (`signal<string | null>`, `signal<boolean>`) in `ApiService`.
   - The navigation bar reactively updates login state, authentication badges, and token status instantly across the UI.
3. **Two-View Single-Page Implementation**:
   - **Catalog View (`app.component.html`)**: Features real-time status filtering tabs (`All`, `Draft`, `Submitted`, `Distributed`), search bar filtering simultaneously across track titles, artist names, genres, and ISRC codes, and an overview metrics row displaying dynamic track counts.
   - **Track Detail & DSP Distribution Matrix (`track-detail.component.ts`)**: Built a dedicated detail view presenting track metadata alongside a dynamic **DSP Distribution Matrix**. Each platform card (Spotify, Apple Music, Anghami) renders platform badges (`pending`, `live`, `rejected`, or `unsubmitted`) and submission timestamps.
4. **Interactive State Transitions & DSP Dispatch**:
   - Multi-select platform dispatch form allowing users to select target DSPs and trigger dispatches (moving `Draft` ➔ `Submitted`).
   - Catalog lifecycle status dropdown enabling direct status transitions (`Draft` ➔ `Submitted` ➔ `Distributed`) and visual confirmation of cascading DSP statuses (`Pending` ➔ `Live`).
5. **1-Click Authentication (Evaluator Experience)**:
   - Designed a persistent navbar header featuring a **"Demo Login"** button that authenticates against `POST /api/auth/login`, stores the JWT Bearer token in `localStorage`, and injects the `Authorization: Bearer` header into all state-modifying requests.
6. **RFC 7807 ProblemDetails Error Handling**:
   - Configured error intercepting that surfaces structured RFC 7807 validation messages and conflict errors cleanly in the UI.

### User Testing & Quality Verification
The User conducted comprehensive testing across both production compilation and live browser execution:
1. **Production Compilation Verification**:
   ```bash
   npm run build
   ```
   Verified that Angular CLI successfully generated the optimized production bundles (`main.js`, `polyfills.js`, `styles.css`) in 2.24s with zero compilation errors, zero warnings, and verified component style budgets in `angular.json`.
2. **Live Browser Interaction (`http://localhost:4200`)**:
   - **Catalog & Search**: Loaded all seeded tracks from the .NET 9 API; verified real-time tab switching between `All`, `Draft`, `Submitted`, and `Distributed` without page refresh; verified instant fuzzy search filtering.
   - **JWT Authentication Flow**: Clicked "1-Click Demo Login", confirmed successful token acquisition, and verified user badge displayed `admin@takwene.com`.
   - **DSP Dispatch & Cascading State**: Opened track details for a `Draft` track, dispatched distributions to Spotify and Apple Music, and verified the status transitioned to `Submitted`. Then marked the track as `Distributed` and confirmed all pending DSP distributions cascaded to `Live`.

---

## 6. Summary & Lessons Learned

| Dimension | Agent Role | User Role |
|---|---|---|
| **Speed & Scaffolding** | Generated 80%+ of boilerplate syntax, templates, and baseline DTOs in minutes. | Filtered and directed the generation, preventing framework bloat and architectural degradation. |
| **Correctness & Reliability** | Often missed ORM state nuances (e.g. Change Tracker key-detection rules, SQLite enum conversions). | Diagnosed underlying ORM mechanics, analyzed SQL query logs, and engineered reliable fixes. |
| **Security & Business Rules** | Defaulted to permissive, unauthenticated endpoints and flat data models. | Designed the music distribution state machine, enforced JWT authentication, and instituted strict input validation. |
| **Quality Assurance & Verification** | Generated isolated test templates without guaranteeing end-to-end suite coherence. | Defined testing gates, executed the full test suite (`dotnet test`), verified zero failures, and tested live API payloads. |
| **Frontend Architecture & UX** | Generated rudimentary static templates without state synchronization or error handling. | Architected modern Angular 19 standalone SPA, designed the DSP distribution matrix, integrated signals-based JWT auth, and verified production builds. |



