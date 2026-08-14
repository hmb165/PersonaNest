# PersonaNest

> *Your home for everything you love.*

A community media journal built with ASP.NET Core MVC. Users log their experience with films,
games, books, anime, manga, TV and music as **Entries** against a shared, community-built
**Media** catalogue.

**Current state: Phase 5 complete — authentication, profiles, settings and the design system.**
Media search, entries and the social features arrive in Phase 6 and later.

---

## Stack

.NET 8 · ASP.NET Core MVC · Entity Framework Core · SQL Server · ASP.NET Core Identity · Bootstrap 5

## Architecture

```
PersonaNest.Web              MVC + API controllers, Razor views, wwwroot
        ↓
PersonaNest.Services         business logic, DTOs, ViewModels, Manual Mapping
        ↓
PersonaNest.Infrastructure   DbContext, Fluent API configurations, repositories, UnitOfWork
        ↓
                             SQL Server

PersonaNest.Domain           entities, enums, abstractions — referenced by all layers
PersonaNest.Tests            xUnit
```

### Project references

| Project | References |
|---|---|
| `PersonaNest.Domain` | *(none)* |
| `PersonaNest.Infrastructure` | Domain |
| `PersonaNest.Services` | Domain, Infrastructure |
| `PersonaNest.Web` | Domain, Services |
| `PersonaNest.Tests` | Domain, Services, Infrastructure |

## Project conventions

- **Mapping strategy: Manual Mapping.** All Entity ↔ DTO / ViewModel conversion is written by
  hand in `PersonaNest.Services/Mapping/`. **AutoMapper is not used.**
- **EF Core Fluent API** via `IEntityTypeConfiguration<T>` classes in
  `PersonaNest.Infrastructure/Data/Configurations/` for all entity, relationship, key, index and
  delete-behaviour configuration. Data annotations are used only for input validation on DTOs and
  ViewModels.
- Controllers stay thin; business logic lives in Services; database access stays behind
  repositories accessed through the Unit of Work.
- All timestamps are UTC and suffixed `…At`.

## Build

```bash
dotnet restore
dotnet build
```

## Database setup

Set the seed passwords once (they are never stored in source control):

```bash
dotnet user-secrets set "Seed:AdminPassword"     "<password>" -p PersonaNest.Web
dotnet user-secrets set "Seed:ModeratorPassword" "<password>" -p PersonaNest.Web
dotnet user-secrets set "Seed:UserPassword"      "<password>" -p PersonaNest.Web
```

Create the initial migration and apply it:

```bash
dotnet ef migrations add InitialCreate \
    -p PersonaNest.Infrastructure -s PersonaNest.Infrastructure
dotnet ef database update \
    -p PersonaNest.Infrastructure -s PersonaNest.Infrastructure
```

Migrations run entirely inside the Infrastructure project via
`PersonaNestDbContextFactory`, so `PersonaNest.Web` never needs a reference to
`PersonaNest.Infrastructure`. Running the Web project in Development also applies pending
migrations and seeds roles plus the three demo accounts automatically.

## Per-phase workflow

Each phase ships as a **complete snapshot** of the solution, not a delta. Extract it *over* the
existing folder — it overwrites changed files and adds new ones, and never deletes anything.

**Do not delete the project folder before extracting.** Your `Migrations/` folder is generated on
your machine and is not in the zip; wiping the folder would lose it and desynchronise the
database. Extracting over the top leaves it untouched.

| Step | When |
|---|---|
| `dotnet user-secrets set …` | **once, ever** — secrets live outside the project folder and survive every extraction |
| `dotnet build` | every phase |
| `dotnet ef migrations add <Name>` | **only when the phase report says the model changed** — never re-run `InitialCreate` |
| `dotnet ef database update` | only after adding a migration |
| `dotnet run --project PersonaNest.Web` | whenever you want to check it |

Phase 2 built the entire schema, so **Phases 3–14 require no migrations**. The next expected ones
are Phase 8b (Genre / MediaGenre) and Phase 15 (Notification). Every phase report states
explicitly whether a migration is needed.

Because extraction never deletes, a phase that renames or removes a file would leave a stale
orphan. Every phase report lists removed files. Phase 5 removed one:
`PersonaNest.Web/Views/Shared/_Layout.cshtml.css` (the MVC template's scoped stylesheet, replaced by
`wwwroot/css/site.css`) — **delete it manually** if it survives your extraction.

Commit after each phase — `git diff` then shows exactly what changed.

## Phase 13 optimisation candidates

Recorded as they are found, deliberately not fixed mid-phase.

| Item | Where | Note |
|---|---|---|
| Two-query page + count | `EntryService.GetMineAsync` | The filter predicate is evaluated twice — once for the page, once for the total. Correct but two round trips. Approved to leave as-is in Phase 4; revisit during the Phase 13 query-optimization pass. |


## Run

```bash
dotnet run --project PersonaNest.Web
```

Then open the HTTPS URL printed in the console (see
`PersonaNest.Web/Properties/launchSettings.json`).

## Test

```bash
dotnet test
```

*No tests exist yet — the test project is scaffolded but empty. Tests arrive in Phase 13.*

---

## Implementation phases

| Phase | Content | Status |
|---|---|---|
| 1 | Solution + four-layer architecture | **complete** |
| 2 | Identity, entities, roles, seed, migrations | **complete** |
| 3 | Repositories + Unit of Work | **complete** |
| 4 | Services + DTOs + ViewModels + Manual Mapping | **complete** |
| 5 | Profiles + Theme / accent customization | **complete** |
| 6 | Media search + community media creation | not started |
| 7 | Entries | not started |
| 8 | Favorites + Collections + Tags | not started |
| 8b | Genre / MediaGenre *(secondary)* | not started |
| 9 | Comments + Likes + Follow + Privacy | not started |
| 10 | Taste Profile + Dashboards | not started |
| 11 | Moderator Applications + Moderation | not started |
| 12 | Serilog + Background Service + API + Swagger | not started |
| 13 | Validation + query optimization + unit tests | not started |
| 14 | Responsive UI + final testing | not started |
| 15 | Notification + SignalR, External API, AI *(bonus)* | not started |

## Authoritative reference documents

- `PersonaNest_Specification_v3.md` — master specification
- `PersonaNest_ERD_v2.drawio` — approved entity-relationship diagram
- `PersonaNest_FINAL_Review.md` — entity / relationship / constraint inventory
- `personanestdesignsystem_v2.html` — design system
