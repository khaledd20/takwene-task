# 🎵 Takwene Track Management — Backend Architecture (Phase 1)

A Track Management and Music Distribution System built with **.NET 9 Web API (Clean Architecture)**, **Entity Framework Core**, and **SQLite**.

The platform is designed to manage artists, track catalogs, and distribution across major Digital Service Providers (DSPs) such as **Spotify**, **Apple Music**, and **YouTube**.

---

## 🏗️ Architecture Overview (Phase 1)

The backend follows **Clean Architecture** principles to ensure strict separation of concerns, maintainability, and testability:

- **`Takwene.Domain`**: Pure enterprise entities (`Artist`, `Track`, `Dsp`, `TrackDistribution`), domain enums (`TrackStatus`, `DistributionStatus`), and domain exceptions with zero third-party dependencies.
- **`Takwene.Application`**: Core business logic, Data Transfer Objects (DTOs), `FluentValidation` request validators, and interface abstractions (`IApplicationDbContext`, `ITrackService`, `IArtistService`, `IJwtTokenService`).
- **`Takwene.Infrastructure`**: EF Core 9 with SQLite provider, entity configurations, code-first migrations, JWT token generation service, and realistic sample data seeding.

---

## 📊 Data Model & Relationships

### 1. Artist
- `Id` (Guid, Primary Key)
- `Name` (Required, Max length 150)
- `Email` (Required, Unique index)
- `Country` (Required, Max length 100)
- Navigation: `ICollection<Track> Tracks`

### 2. Track
- `Id` (Guid, Primary Key)
- `Title` (Required, Max length 200)
- `ArtistId` (Guid, Foreign Key -> Artist)
- `Isrc` (Required, ISO 3901 12-character alphanumeric code, Unique index)
- `ReleaseDate` (DateTime UTC)
- `Genre` (Required, Max length 80)
- `Status` (Enum: `Draft`, `Submitted`, `Distributed`)
- Navigation: `Artist Artist`, `ICollection<TrackDistribution> Distributions`

### 3. DSP (Digital Service Provider)
- `Id` (Guid, Primary Key)
- `Name` (e.g. `Spotify`, `Apple Music`, `YouTube`)
- Navigation: `ICollection<TrackDistribution> Distributions`

### 4. TrackDistribution
- `Id` (Guid, Primary Key)
- `TrackId` (Guid, Foreign Key -> Track)
- `DspId` (Guid, Foreign Key -> DSP)
- `SubmittedAt` (DateTime UTC)
- `Status` (Enum: `Pending`, `Live`, `Rejected`)
- Unique composite index on `(TrackId, DspId)` to prevent duplicate provider distributions.

---

## 💾 Migrations & Seed Data

The project utilizes EF Core Code-First migrations with an initial migration (`InitialCreate`) pre-generated. When launched, the database automatically seeds:
- **4 Artists**: The Weeknd, Dua Lipa, Daft Punk, Amr Diab
- **3 DSPs**: Spotify, Apple Music, YouTube
- **10 Tracks** with mixed statuses (`draft`, `submitted`, `distributed`)
- **Track Distributions** across DSPs with `live`, `pending`, and `rejected` statuses

---

## 🛠️ Status & Next Milestones

- ✅ **Domain Layer**: Complete with entities, enums, and domain exception contracts.
- ✅ **Application Layer**: Complete with DTOs, service interfaces, and FluentValidation rules.
- ✅ **Infrastructure Layer**: Complete with EF Core DbContext, SQLite provider, migrations, and database seeder.
- 🔄 **Next**: REST API Controllers, JWT Authorization, Unit Tests, and React Single-Page Application.
