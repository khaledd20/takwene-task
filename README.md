# 🎵 Takwene Track Management & Music Distribution Platform

A full-stack Track Management and Music Distribution System built with **.NET 9 Web API (Clean Architecture)**, **Entity Framework Core**, **SQLite**, and a modern **Angular 19** standalone single-page application.

The platform simulates a music distribution company managing artists and catalog tracks, orchestrating releases and live distribution statuses across major Digital Service Providers (DSPs) such as **Spotify**, **Apple Music**, and **YouTube**.

---

## 🏛️ System Architecture

The backend strictly adheres to **Clean Architecture** (Onion / Hexagonal pattern) with unidirectional dependencies:

```
takwene_task/
├── backend/
│   ├── src/
│   │   ├── Takwene.Domain/             # Pure domain entities, Enums, Domain Exceptions (zero external dependencies)
│   │   ├── Takwene.Application/        # DTOs, Service Contracts, FluentValidation, Business Logic
│   │   ├── Takwene.Infrastructure/     # EF Core DbContext, Migrations, SQLite, Database Seeder, JWT Service
│   │   └── Takwene.Api/                # REST Controllers, JWT Bearer Auth, ProblemDetails Middleware, Swagger UI
│   └── tests/
│       └── Takwene.UnitTests/          # Relational SQLite in-memory Unit Tests (xUnit, FluentAssertions)
└── frontend/
    └── src/
        └── app/
            ├── components/             # StatusBadge, TrackDetail (DSP Matrix & Simulator), CreateTrackModal, CreateArtistModal
            ├── models/                 # Strong TypeScript contracts matching backend DTOs
            ├── services/               # ApiService with reactive Signals and JWT Bearer auth
            ├── app.component.ts        # Main catalog view with real-time status tabs, search & CSV export
            └── styles.css              # Custom Dark-Mode Design System
```

---

## ⚡ Tech Stack

- **Backend**: .NET 9 (C# 13, ASP.NET Core Web API)
- **Data Access & ORM**: Entity Framework Core 9.0 (SQLite Provider)
- **Validation**: FluentValidation (Compiled ISO 3901 ISRC Regex, Domain Rules)
- **Authentication**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **API Documentation**: Swagger / OpenAPI (`Swashbuckle.AspNetCore`) with Bearer Auth UI
- **Frontend**: Angular 19 (Standalone Components, Signals, HttpClient with `fetch`)
- **Styling**: Modern Vanilla CSS Design System (Inter font, dark glassmorphism, responsive)
- **Testing**: xUnit + FluentAssertions (in-memory SQLite relational tests, 14 passing tests)

---

## 🚀 Quick Start Guide

### Prerequisites:
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)

### 1. Run Backend API:
```bash
cd backend/src/Takwene.Api
dotnet run --urls "http://localhost:5000"
```
- API Base URL: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

*(The database `takwene.db` is automatically created, migrated, and seeded with 4 artists, 10 tracks, and 3 DSPs on first run!)*

### 2. Run Angular Frontend:
```bash
cd frontend
npm install
npm start
```
- Web Client: `http://localhost:4200`

---

## 💾 How to Run EF Core Migrations Manually (Optional)

Database migrations apply automatically on application startup. To apply or inspect migrations manually via the .NET CLI:

```bash
dotnet ef database update --project backend/src/Takwene.Infrastructure --startup-project backend/src/Takwene.Api
```

To add a new migration:
```bash
dotnet ef migrations add <MigrationName> --project backend/src/Takwene.Infrastructure --startup-project backend/src/Takwene.Api
```

---

## 🔐 How to Obtain and Use a JWT Token

### In the Angular Frontend (1-Click Experience):
Click the **"🔑 1-Click Demo Login"** button in the top navbar. It automatically calls `POST /api/auth/login`, saves the token to `localStorage`, and updates the header badge to `admin@takwene.com`. All subsequent state-modifying requests automatically attach the Bearer token.

### In Swagger UI:
1. Open `http://localhost:5000/swagger`.
2. Expand `POST /api/auth/login` and click **Try it out**.
3. Use default seeded credentials:
   ```json
   {
     "email": "admin@takwene.com",
     "password": "Password123!"
   }
   ```
4. Click **Execute** and copy the returned `token` string.
5. Click the green **Authorize 🔓** button at the top right of Swagger, paste the token as `Bearer <paste-token>`, and click **Authorize**.

---

## 📡 API Reference Table

| Method | Route | Auth Required | Description |
|---|---|:---:|---|
| **POST** | `/api/auth/login` | No | Authenticate and obtain JWT token |
| **GET** | `/api/auth/me` | **Yes** | Verify current authenticated user token |
| **POST** | `/api/artists` | No | Create an artist (Name, Email, Country) |
| **GET** | `/api/artists` | No | List all artists with track counts |
| **POST** | `/api/tracks` | **Yes** | Create a track for an artist |
| **GET** | `/api/tracks` | No | List tracks with filters: `?artistId=&genre=&status=` |
| **GET** | `/api/tracks/{id}` | No | Get track details including DSP distribution matrix |
| **POST** | `/api/tracks/{id}/distribute` | **Yes** | Submit a track to one or more DSPs |
| **PATCH** | `/api/tracks/{id}/status` | **Yes** | Update a track's catalog status |
| **PATCH** | `/api/tracks/{id}/distributions/{dspId}/status` | **Yes** | **(New)** Update/simulate DSP status (`live` or `rejected` with reason) |
| **GET** | `/api/tracks/export` | No | **(New)** Export entire catalog & DSP statuses as CSV |
| **GET** | `/api/dsps` | No | List configured DSP providers |

---

## 🧪 Running Automated Unit Tests

```bash
dotnet test backend/Takwene.sln
```

Executes **14 unit tests** in ~1s against an in-memory SQLite relational provider:
- **Entity & Validation rules**: ISO 3901 ISRC format verification, duplicate ISRC conflict prevention, duplicate artist email rejection.
- **State Machine transitions**: Draft ➔ Submitted upon dispatch; cascading pending distributions to Live upon marking Distributed.
- **DSP Review & Rejection Workflow**: Approving to Live with auto-cascade to Distributed, rejecting with required reason, and validation failure when reason is omitted.
- **Catalog CSV Export**: Verifying RFC-4180 format, headers, escaped special characters, and accurate DSP aggregations.

---

## 📄 Engineering Reflection & AI Collaboration

Detailed architectural rationale, EF Core ChangeTracker concurrency debugging, security remediations, and live testing evidence are documented in [DECISIONS.md](file:///k:/SELF%20PROJECTS/takwene_task/DECISIONS.md).

---

## 📝 Submission Links for Google Drive Doc

In your Google Drive Doc (`KHALED_MOHAMED_FULLSTACKDEVELOPER`), paste:
1. **01_ Project link**: `https://github.com/khaledd20/takwene-task`
2. **02_How to run the project**: `https://github.com/khaledd20/takwene-task#readme`
3. **03_Vibe coding challenges**: `https://github.com/khaledd20/takwene-task/blob/main/DECISIONS.md`
