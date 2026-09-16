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
| **Automated Testing & Verification** | Generated individual test file templates without ensuring solution-wide execution. | **User Intervention**: Mandated automated test execution as a strict quality gate before commits. Executed `dotnet test backend/Takwene.sln`, validating 14 passing unit tests across domain rules, state transitions, rejection callbacks, and SQLite in-memory relational integrity. |
| **DSP Ingestion & Rejection Callbacks** | Initially modeled DSP distribution as a static, write-only status. | **User Intervention**: Architected realistic DSP rejection review simulation. Added rejection reasons, review timestamps, validation rules requiring rejection notes, and interactive approval/rejection simulation on the UI. |
| **Catalog Auditing & Reporting** | Left catalog data accessible only via paginated JSON API queries. | **User Intervention**: Designed `GET /api/tracks/export` producing RFC-4180-compliant CSV files with ISRC codes, artist info, and DSP distribution coverage metrics, triggered via 1-click download in the UI. |

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
All **14 automated unit tests** executed and passed cleanly with zero failures:
```
Passed! - Failed: 0, Passed: 14, Skipped: 0, Total: 14, Duration: 1 s - Takwene.UnitTests.dll (net9.0)
```

The User verified the following critical business rules across the entire test suite:
1. **Catalog Drafting (`CreateTrackAsync`)**: Verifies tracks initialize in `draft` status with valid metadata and relational artist mapping.
2. **Relational & ISO 3901 Invariants**: Verifies duplicate ISRC codes immediately throw `ConflictException` (409) and invalid formats trigger `ValidationException` (400).
3. **State Machine Integrity**:
   - Dispatching a draft track automatically transitions its catalog status to `submitted` with `pending` distributions.
   - Dispatching an already submitted or distributed track prevents illegal status reversals.
4. **Cascading State Transitions**: Marking a track as `distributed` cascades all pending DSP distributions to `live` simultaneously.
5. **Artist Uniqueness & Constraints**: Verifies artist creation and duplicate email rejections.
6. **DSP Approval to Live (`UpdateDistributionStatusAsync`)**: Verifies that setting a distribution to `live` updates its status, clears previous rejection reasons, stamps `ReviewedAt`, and automatically cascades the Track catalog status to `distributed` once all its target DSP distributions are Live.
7. **DSP Rejection with Reason**: Verifies that setting a distribution to `rejected` records the specific failure reason, sets `ReviewedAt`, and preserves track integrity.
8. **Rejection Validation Invariants**: Verifies that submitting a rejection without an explanatory reason immediately fails validation (`DomainException` / 400 Bad Request).
9. **Catalog CSV Report Generation (`ExportCatalogCsvAsync`)**: Verifies RFC-4180 compliance, sanitization of special characters, presence of standard headers, and accurate aggregation of `TotalDsps`, `LiveDsps`, `PendingDsps`, and `RejectedDsps`.

**User vs. Agent Distinction**: While the Agent drafted initial test method structures, the User established the test matrix in `Demo.md`, executed `dotnet test`, inspected the output, and ensured 100% test pass rate across all 14 tests before green-lighting commits.

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
   - **Track Detail & DSP Distribution Matrix (`track-detail.component.ts`)**: Built a dedicated detail view presenting track metadata alongside a dynamic **DSP Distribution Matrix**. Each platform card (Spotify, Apple Music, YouTube) renders platform badges (`pending`, `live`, `rejected`, or `unsubmitted`) and submission timestamps.
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
   Verified that Angular CLI successfully generated the optimized production bundles (`main.js`, `polyfills.js`, `styles.css`) in 9.28s with zero compilation errors, zero warnings, and verified component style budgets in `angular.json`.
2. **Live Browser Interaction (`http://localhost:4200`)**:
   - **Catalog & Search**: Loaded all seeded tracks from the .NET 9 API; verified real-time tab switching between `All`, `Draft`, `Submitted`, and `Distributed` without page refresh; verified instant fuzzy search filtering.
   - **JWT Authentication Flow**: Clicked "1-Click Demo Login", confirmed successful token acquisition, and verified user badge displayed `admin@takwene.com`.
   - **DSP Dispatch & Cascading State**: Opened track details for a `Draft` track, dispatched distributions to Spotify and Apple Music, and verified the status transitioned to `Submitted`. Then marked the track as `Distributed` and confirmed all pending DSP distributions cascaded to `Live`.

---

## 6. Continuous Feature Iteration: DSP Rejection Workflow & Catalog CSV Export

To demonstrate sustained software evolution beyond the minimum viable spec, the User designed and guided the implementation of two enterprise-grade music distribution features:

### Feature 1: DSP Distribution Rejection & Review Callback Simulator
- **Industry Context**: In production distribution ecosystems (such as FUGA, DistroKid, or TuneCore), DSP ingestion is asynchronous and non-deterministic. Platforms perform audio fingerprinting, metadata validation, and copyright audits. DSPs frequently reject submissions for non-compliant bitrates, explicit language mismatches, or metadata discrepancies.
- **Architectural Implementation**:
  - **Domain Model**: Extended `TrackDistribution` with `RejectionReason` (nvarchar 500) and `ReviewedAt` (nullable UTC timestamp).
  - **Infrastructure**: Added and executed EF Core migration `20260916165422_AddDistributionRejectionAndReview`.
  - **Application Service & Validator**: Implemented `UpdateDistributionStatusRequestValidator` enforcing that rejections cannot be created without an explicit audit reason. In `TrackService.UpdateDistributionStatusAsync`, transitioning all target DSPs to `Live` automatically updates the parent track's catalog status to `Distributed`.
  - **API Contract**: Exposed `PATCH /api/tracks/{id}/distributions/{dspId}/status` guarded by JWT authorization.
  - **Frontend UI & Simulator**: Each DSP card features interactive simulation controls (`✓ Set Live` and `✕ Reject`), visual alert borders, and structured rejection reason callouts.

### Feature 2: Catalog CSV Export
- **Industry Context**: Operations and catalog management teams require bulk audit reports to reconcile DSP distribution coverage and verify ISRC registries.
- **Architectural Implementation**:
  - **Application Service**: Added `ExportCatalogCsvAsync` to `ITrackService`, generating RFC-4180-compliant CSV data streams with escaped quotes and aggregated counts (`TotalDsps`, `LiveDsps`, `PendingDsps`, `RejectedDsps`).
  - **API Contract**: Exposed `GET /api/tracks/export` returning `text/csv` with timestamped filename attachment headers.
  - **Frontend UI**: Integrated a 1-click **"📥 Export CSV"** action button in the Angular navigation header that streams the file directly to the user's browser.

---

### Empirical Evidence: User Manual Live Testing (Browser Screenshot Analysis)

The User performed hands-on exploratory and boundary testing in the live running application (`http://localhost:4200`), verifying real-time status transitions and UI responsiveness:

> 📸 **Live UI Verification (DSP Distribution Matrix in Action)**:
> In browser testing, the User opened the Track Detail modal for track *"Solar Power"* (ISRC: `USUG12209999`) and verified the real-time DSP matrix, approval/rejection simulation, and state cascades:

#### Specific Test Scenarios Verified by User:
1. **Target Entity**: Track **"Solar Power"** by **The Weeknd** (ISRC: `USUG12209999`, Genre: `Indie Pop`, Release Date: `Sep 15, 2026`).
2. **Initial State**: Track catalog status displayed as **`SUBMITTED`**.
3. **Simulated DSP Approval (Apple Music)**:
   - User triggered approval via **"✓ Set Live"**.
   - Result: Badge switched to **`LIVE`** (emerald green), stamped with exact `Submitted: 9/16/26, 5:13 PM` and `Reviewed: 9/16/26, 5:13 PM` timestamps.
   - Action dynamically transformed to **"✕ Reject"** for reversible review simulation.
4. **Simulated DSP Rejection (Spotify)**:
   - User triggered rejection and supplied rejection reason: `"Metadata does not match audio recording standards."`.
   - Result: Card rendered with distinct red warning border (`.dsp-rejected`), badge displayed **`REJECTED`** (crimson), review timestamp stamped (`Reviewed: 9/16/26, 5:13 PM`), and a structured callout surfaced:
     > **REJECTION REASON:** Metadata does not match audio recording standards.
   - Action dynamically offered **"✓ Set Live"** to simulate post-remediation re-approval.
5. **Clean Unsubmitted State (YouTube)**:
   - YouTube card correctly displayed badge **`UNSUBMITTED`** with descriptive label *"Not dispatched yet"*.
6. **Reactive UI Feedback**:
   - The top banner dynamically updated with green toast: `"DSP status successfully updated to live."`
7. **Dispatch & Lifecycle Controls**:
   - Verified the **"Dispatch to DSPs"** multi-checkbox form and **"Update Catalog Status"** dropdown (`Submitted` ➔ `Apply Status`).

---

## 7. Summary & Lessons Learned

| Dimension | Agent Role | User Role |
|---|---|---|
| **Speed & Scaffolding** | Generated 80%+ of boilerplate syntax, templates, and baseline DTOs in minutes. | Filtered and directed the generation, preventing framework bloat and architectural degradation. |
| **Correctness & Reliability** | Often missed ORM state nuances (e.g. Change Tracker key-detection rules, SQLite enum conversions). | Diagnosed underlying ORM mechanics, analyzed SQL query logs, and engineered reliable fixes. |
| **Security & Business Rules** | Defaulted to permissive, unauthenticated endpoints and flat data models. | Designed the music distribution state machine, enforced JWT authentication, and instituted strict input validation. |
| **Continuous Iteration** | Initially stopped at basic CRUD operations without real-world edge cases. | Architected realistic DSP rejection callbacks and catalog CSV export, elevating the project to industry standard. |
| **Quality Assurance & Verification** | Generated isolated test templates without guaranteeing end-to-end suite coherence. | Defined testing gates, executed the full test suite (`dotnet test`), verified all 14 tests passing, and conducted live UI testing with empirical proof. |
| **Frontend Architecture & UX** | Generated rudimentary static templates without state synchronization or error handling. | Architected modern Angular 19 standalone SPA, designed the DSP distribution matrix, integrated signals-based JWT auth, and verified production builds. |




