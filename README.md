# PersonaNest

> *Your home for everything you love.*

A community media journal built with ASP.NET Core MVC. Users log their experience with films,
games, books, anime, manga, TV and music as **Entries** against a shared, community-built
**Media** catalogue.

**Current state: all bonus requirements complete** — real-time notifications (SignalR), external
API integrations (TMDB, RAWG, Jikan, Google Books, MusicBrainz — one per media category), and an
AI feature (Anthropic-generated taste narrative). See "Bonus features" below for setup (TMDB,
RAWG and Anthropic need an API key you provide via `dotnet user-secrets`; the rest work keyless).
A large UI/UX polish pass followed (new logo, redesigned navbar, separate Edit/Settings pages,
rotating hero imagery, theme picker, and several real bugs fixed along the way) - see git history
for the full list.

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

Phase 2 built the schema; Phase 15 added the second migration, `AddNotifications` (the
`Notifications` table). Every phase report states explicitly whether a migration is needed.

Because extraction never deletes, a phase that renames or removes a file would leave a stale
orphan. Every phase report lists removed files. Phase 5 removed one:
`PersonaNest.Web/Views/Shared/_Layout.cshtml.css` (the MVC template's scoped stylesheet, replaced by
`wwwroot/css/site.css`) — **delete it manually** if it survives your extraction.

Commit after each phase — `git diff` then shows exactly what changed.

## Phase 13 — validation, query optimization, unit tests

Completed. Highlights (full report delivered in-session):

- **Validation**: added `Enum.IsDefined` checks (Entry Status/Privacy, Collection Privacy, Report
  Reason) that data annotations alone don't catch on out-of-range values bound from raw ints;
  added server-side length/required checks for `AdminController.Ban`'s reason and the
  moderation/application notes fields, which bind as raw parameters rather than a validated DTO.
- **Query optimization**: `EntryService.GetMineAsync`'s duplicated filter predicate is now
  single-sourced. Every `Skip`/`Take` query now has a deterministic `OrderBy`. Two real bugs found
  in the process: `ProfileService`'s `AverageRating` and `TasteProfileCalculator`'s entry fetch
  were both silently capped at 100 rows by `Repository<T>.MaxPageSize` despite asking for more -
  fixed via a proper SQL `AVG()` and a paging loop, respectively. `CommentService.GetForEntryAsync`
  had the same cap bug, fixed the same way.
- **EF diagnostics**: the global-query-filter/required-navigation warning turned out to be a real
  bug, not noise - `CollectionItem`→`Media` and `Favorite`→`Media` are both required navigations
  into a soft-delete-filtered entity, so a plain projected query silently dropped a user's
  collection item / favorite the moment its media was later removed (reproduced live, then fixed
  via `ICollectionRepository`/`IFavoriteRepository` using `IgnoreQueryFilters`). The MARS/savepoints
  warning turned out to be unnecessary boilerplate - nothing in the codebase needs concurrent
  result sets on one connection - so `MultipleActiveResultSets=true` was removed from both
  connection strings, restoring EF's automatic savepoint rollback within a transaction.
- **Unit tests**: 48 tests added in `PersonaNest.Tests/Services/`, covering Entry/Favorite/
  Collection/Comment/Follow business rules, `TasteProfileCalculator`'s actual arithmetic, and
  Admin/Report moderation rules. All passing.

## Phase 14 — responsive UI, mobile navigation, final polish

Completed. CSS/Razor-markup only — no `.cs` file was touched, no schema change, no new package.
Audited all 15 page groups at 375px/768px/1280px; found and fixed 8 real responsive bugs:

- **No mobile navigation at all below 992px** — the design system's own "Responsive Breakpoints"
  section documents a hamburger navbar as required `<768px` behaviour; it didn't exist. Added a
  `navbar-toggler` button plus a `#mobileNav` collapse panel (`_Layout.cshtml`) reusing Bootstrap's
  already-loaded Collapse plugin (`data-bs-toggle="collapse"`) — no new script or dependency.
- **`.table-wrap { overflow: hidden }`** was clipping table content on narrow screens instead of
  scrolling it, directly contradicting the design system's documented "Tables: horizontal scroll"
  mobile spec. Changed to `overflow-x: auto`.
- **`Entries/Details.cshtml`** used a hard-coded inline `grid-template-columns: 1fr 280px`, so the
  sidebar never collapsed on mobile. Extracted to a `.entry-detail-layout` class with a `768px`
  media query collapsing it to one column.
- **`.admin-layout`**'s 200px fixed sidebar didn't respond at all below 1024px, and fixing the
  grid-column collapse alone wasn't enough — `.admin-main` (a grid item) still overflowed because
  of CSS Grid's `min-width: auto` default, which stops a `1fr` track shrinking below its content's
  intrinsic width. Fixed with `grid-template-columns: 1fr` at 1024px, sidebar converted to a
  horizontal scroller, and `min-width: 0` added to `.admin-main`.
- **`.profile-tabs`** and **`.profile-stats`** both overflowed the viewport on mobile with no
  scroll handling; both given `overflow-x: auto` plus `flex-shrink: 0` on their items.
- **`.auth-panel`** had no reduced padding on very small screens (≤480px); dead CSS for a
  never-used `.auth-page` class was removed; a redundant `.navbar-nav { display: none }` rule was
  removed since Bootstrap's own `d-none d-lg-flex` utility on the element already did the job.

Verified the professor's "responsive Bootstrap theme" requirement explicitly: viewport meta tag
present, Bootstrap 5 loaded and its utilities/Collapse plugin actively used, confirmed rendering
correctly across breakpoints.

## Phase 15 — real-time notifications (SignalR)

Completed. New `Notification` entity + `AddNotifications` migration, layered the same way as
every other feature: `NotificationsController`/`NotificationHub` (Web) → `NotificationService`
(Services) → `INotificationRepository`/generic repositories (Infrastructure) → SQL Server.

- **Triggers**: a new follower, someone liking your entry, a top-level comment on your entry, and
  a reply to your comment. Each skips notifying yourself (liking/commenting on your own entry,
  replying to your own comment). Moderation-related notifications (bans, report resolutions,
  moderator application decisions) were **not** included - flagging this as a scope question for
  a future phase rather than guessing at it.
- **Delivery**: `PersonaNest.Services` defines `INotificationBroadcaster`, an abstraction with no
  ASP.NET Core dependency; `PersonaNest.Web` implements it (`SignalRNotificationBroadcaster`) over
  `IHubContext<NotificationHub>`, so the Services layer still never references hosting/SignalR
  types - the same boundary the project has held since Phase 1. `NotificationHub` itself is
  server-to-client push only (no client-invokable methods); `Context.UserIdentifier` resolves via
  SignalR's default `IUserIdProvider`, which already reads the same `ClaimTypes.NameIdentifier`
  claim every controller reads through `ClaimsPrincipalExtensions.GetUserId` - no custom provider
  needed.
- **Progressive enhancement (§12)**: the bell (a `ViewComponent`, server-rendered) and the
  `/Notifications` history page both work with JavaScript off. `wwwroot/js/notifications.js` only
  makes new notifications arrive live on top of that, over a vendored
  `wwwroot/lib/signalr/signalr.min.js` (Microsoft's official browser client, matching this
  project's existing manually-vendored-library convention rather than a package manager).
- **Verified live**: with a browser tab authenticated as one demo account holding an open
  WebSocket to `/hubs/notifications`, a second, independent HTTP session (PowerShell,
  antiforgery-token-correct) logged in as another demo account and posted a real Follow action.
  The first tab's unread badge and dropdown list updated with no page reload, confirming delivery
  over an actual WebSocket connection rather than merely a persisted DB row. Mark-as-read and mark
  all read were also verified live, and mobile layout was re-checked for the new bell icon
  (no regression to the Phase 14 responsive work).
- **Tests**: 8 new tests in `PersonaNest.Tests/Services/NotificationServiceTests.cs` (self-skip
  rules, read-state operations) plus 4 new tests in the existing Follow/Entry/Comment service test
  files verifying the notification hooks fire with the right arguments - 58 total, all passing.

## Bonus features — external APIs (one per category) + AI (Anthropic)

All are read-only integrations over plain `HttpClient` (no SDK package for any of them - just
their REST APIs), living entirely in `PersonaNest.Services` since none needs an ASP.NET Core
hosting type the way SignalR's `IHubContext` did. Every one degrades gracefully with no API key
configured (or a failed call): the affected search just returns an empty list - the Add Media form
still works manually - and a missing/failing AI call means the Taste Profile card simply doesn't
show the extra paragraph. Neither ever produces a 500 or breaks the background service's other work.

**Setup** - set your own keys locally, the same way the seed passwords already work (never
committed to source control). Only three of the five search providers need a key:

```bash
dotnet user-secrets set "TMDb:ApiKey"      "<your TMDB v3 API key>"      -p PersonaNest.Web
dotnet user-secrets set "Rawg:ApiKey"      "<your RAWG API key>"         -p PersonaNest.Web
dotnet user-secrets set "Anthropic:ApiKey" "<your Anthropic API key>"    -p PersonaNest.Web
```

TMDB keys are free at themoviedb.org (account → Settings → API). RAWG keys are free at rawg.io/apidocs.
Anthropic keys are issued at console.anthropic.com. Jikan (MyAnimeList), Google Books and
MusicBrainz all work with **no key at all** (Google Books and Jikan run on shared, low keyless rate
limits - fine for occasional interactive searches, but expect the odd 429/504 under load).

- **One search provider per category**, all wired into the same Add Media auto-fill panel
  (`wwwroot/js/external-search.js`, progressive enhancement - hidden entirely without JS, manual
  entry always works):

  | Category | Provider | Service |
  |---|---|---|
  | Games | RAWG | `IRawgService`/`RawgService` |
  | Movies / TV Shows | TMDB | `ITmdbService`/`TmdbService` |
  | Anime / Manga | Jikan (MyAnimeList) | `IJikanService`/`JikanService` |
  | Books | Google Books | `IGoogleBooksService`/`GoogleBooksService` |
  | Music | MusicBrainz | `IMusicBrainzService`/`MusicBrainzService` |

  Selecting a result fills Title, Release Year, Description and Cover Image URL where the
  provider has them (RAWG's search-list endpoint has no description; MusicBrainz's cover art is a
  best-effort Cover Art Archive URL that 404s silently if none exists). The search itself is
  proxied through `MediaController.SearchExternal` (`[Authorize]`, not part of the
  Swagger-documented `api/` surface) so no provider key ever reaches the browser. All five share
  one response shape, `ExternalSearchResultDto`.
- **AI (`IAiNarrativeGenerator`/`AnthropicNarrativeGenerator`)**: a 2-3 sentence personalized
  paragraph built from the same `TasteProfileDto` the Taste Profile card already renders (top
  categories, top tags, average rating, most active month). Generated by the existing Phase 12
  background service - right after a user's taste-profile stats refresh, `TasteProfileCalculator
  .RefreshNarrativeAsync` calls the AI and persists the result onto the `TasteProfile` row
  (`AiNarrative`/`AiNarrativeGeneratedAt`, migration `AddTasteProfileAiNarrative`), so pages read a
  cached paragraph instead of calling the AI on every view. A configurable freshness window
  (`Ai:NarrativeRefreshIntervalHours`, default 24h) stops it from being regenerated on every
  15-minute refresh cycle. Shown on both Dashboard and Profile via the shared `_TasteProfile.cshtml`
  partial.
- **Tests**: `TmdbServiceTests.cs` and `AnthropicNarrativeGeneratorTests.cs` (missing-key
  degrade-gracefully path, real JSON-response mapping, HTTP-failure handling, via a fake
  `HttpMessageHandler` - no real network calls in the test suite) plus 4 new
  `TasteProfileCalculatorTests` covering the freshness-window skip logic - 72 total, all passing.
- **Verified live**: with no keys configured, app starts cleanly and every provider degrades to
  `200 []` rather than an error. With no key needed, MusicBrainz search was verified end-to-end
  live against the real API (10 real results parsed correctly, including cover art URLs); Google
  Books and Jikan hit transient 429/504 responses from their shared keyless tier during testing,
  both handled by the same graceful-empty-list path with no server error. Full verification of the
  keyed providers (TMDB, RAWG) and the AI narrative needs the keys above.

## Run

```bash
dotnet run --project PersonaNest.Web
```

Then open the HTTPS URL printed in the console (see
`PersonaNest.Web/Properties/launchSettings.json`).

## Logging (Serilog, §24)

Every `ILogger<T>` in every layer (Web, Services, Infrastructure) is routed through Serilog -
configured once in `PersonaNest.Web/Program.cs` via `Host.UseSerilog(...)`, reading the
`"Serilog"` section of `appsettings.json` (base config: Console + rolling daily file under
`PersonaNest.Web/Logs/`, `Information` minimum level) and `appsettings.Development.json`
(overrides to `Debug`). No code elsewhere references Serilog directly - change sinks/levels by
editing that JSON, no recompile needed for level changes at runtime restart.

Notably logged: sign-in success/failure/lockout and registration (`AuthController`); the
development password-reset email, including the reset link (`DevelopmentEmailSender` -
`IEmailSender`'s only implementation, since there is no production SMTP configuration in scope);
ban/unban/promote/demote/report-resolve-dismiss actions and their failures (`AdminService`); one
line per HTTP request (`UseSerilogRequestLogging`); and the background service's per-cycle
summaries and per-item failures (`PersonaNestBackgroundService`).

## Background service (§10, §26, D-20)

One hosted service, `PersonaNest.Services.BackgroundServices.PersonaNestBackgroundService`, runs
two independent tasks concurrently, each on its own configurable interval (`appsettings.json`):

| Task | Interval config | Default |
|---|---|---|
| Taste-profile refresh (§22) | `TasteProfile:RefreshIntervalMinutes` | 15 minutes |
| Media aggregate reconciliation (D-20) | `MediaAggregates:ReconciliationIntervalHours` | 24 hours |

Both run once immediately at startup, then on their interval. Media aggregate reconciliation is
the *safety net* - the primary path is the synchronous recompute already inside
`EntryService.CreateAsync`/`UpdateAsync`/`DeleteAsync` (via `IMediaRepository.RecalculateAggregatesAsync`),
which keeps `Media.AverageRating`/`RatingCount`/`EntryCount` correct on every write, counting only
`Privacy = Public`, non-deleted entries. The nightly task calls that exact same repository method
for every media row, so it can only ever agree with the synchronous path, never introduce a second
rule.

## REST API + Swagger (§25)

Read-oriented JSON endpoints for external consumers, layered `ApiController → Service →
IUnitOfWork → Repository → EF Core → SQL Server` - never `DbContext` directly, and every response
is a DTO already used by the MVC site, never an EF entity.

| Endpoint | Auth | Notes |
|---|---|---|
| `GET /api/categories` | none | The 7 media categories |
| `GET /api/media?query=&categoryId=&page=&pageSize=` | none | Paged media search |
| `GET /api/media/{id}` | none | Media detail; 404 if not found |
| `GET /api/entries/mine` | cookie (`[Authorize]`) | The caller's own entries only |

Authentication is the same cookie scheme as the rest of the site - there is no separate token
issuer. An unauthenticated request to `/api/entries/mine` gets a real `401` (not the MVC site's
302-to-login redirect: `ConfigureApplicationCookie`'s `OnRedirectToLogin`/`OnRedirectToAccessDenied`
events special-case `/api` paths to return 401/403 instead). Swagger UI, opened Development-only
at `/swagger`, shares the browser's session cookie automatically when opened from a signed-in tab,
so "Try it out" against `/api/entries/mine` exercises the same authorization the MVC site enforces.

Endpoint set is not spec-mandated beyond "REST API + Swagger with DTOs only" (§25) - this scope
(catalogue browse + the caller's own entries) was chosen to demonstrate both an anonymous, public
surface and an authenticated one without duplicating the whole MVC action set.

## Test

```bash
dotnet test
```

58 tests across `PersonaNest.Tests/Services/` (Phase 13 added the first 48; Phase 15 added 10 more
covering notifications).

---

## Implementation phases

| Phase | Content | Status |
|---|---|---|
| 1 | Solution + four-layer architecture | **complete** |
| 2 | Identity, entities, roles, seed, migrations | **complete** |
| 3 | Repositories + Unit of Work | **complete** |
| 4 | Services + DTOs + ViewModels + Manual Mapping | **complete** |
| 5 | Profiles + Theme / accent customization | **complete** |
| 6 | Media search + community media creation | **complete** |
| 7 | Entries | **complete** |
| 8 | Favorites + Collections + Tags | **complete** |
| 8b | Genre / MediaGenre *(secondary)* | cut — see decision D-8 |
| 9 | Comments + Likes + Follow + Privacy | **complete** |
| 10 | Taste Profile + Dashboards | **complete** |
| 11 | Moderator Applications + Moderation | **complete** |
| 12 | Serilog + Background Service (2 tasks) + REST API + Swagger | **complete** |
| 13 | Validation + query optimization + unit tests | **complete** |
| 14 | Responsive UI + final testing | **complete** |
| 15 | Notification + SignalR | **complete** |

**Bonus/extra requirements** (tracked separately from the core phase sequence, per professor's
requirements): SignalR — **complete** (Phase 15, above). Consume an External API (TMDB) and AI
(Anthropic taste narrative) — **complete** (see "Bonus features", above). All three bonus
requirements are now done.

## Authoritative reference documents

- `PersonaNest_Specification_v3.md` — master specification
- `PersonaNest_ERD_v2.drawio` — approved entity-relationship diagram
- `PersonaNest_FINAL_Review.md` — entity / relationship / constraint inventory
- `personanestdesignsystem_v2.html` — design system
