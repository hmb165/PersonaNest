using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonaNest.Infrastructure;
using PersonaNest.Infrastructure.Seed;
using PersonaNest.Services.BackgroundServices;
using PersonaNest.Services.Implementations;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services;

/// <summary>
/// The single registration surface PersonaNest.Web talks to.
/// <para>
/// Web calls <see cref="AddApplicationServices"/>; this calls <c>AddInfrastructure</c> in turn,
/// which brings in the DbContext, Identity, the repositories and the Unit of Work. Web therefore
/// never references PersonaNest.Infrastructure, and the approved dependency direction
/// Web -&gt; Services -&gt; Infrastructure holds (approved option (a), Phase 1 report §5.1).
/// </para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);

        // ── Phase 4: business logic (§10) ────────────────────────────────────────────────
        // Scoped, matching the Unit of Work they depend on: one unit of work, one DbContext and
        // one change tracker per request. Each service is narrow - no single giant service
        // (rule 6).
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IEntryService, EntryService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IFollowService, FollowService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IModeratorApplicationService, ModeratorApplicationService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ITasteProfileCalculator, TasteProfileCalculator>();

        // ── Phase 15: real-time notifications (SignalR) ──────────────────────────────────
        // INotificationBroadcaster is registered by PersonaNest.Web (it wraps IHubContext, an
        // ASP.NET Core hosting type this layer must not reference).
        services.AddScoped<INotificationService, NotificationService>();

        // ── Bonus: consume external APIs (one per media category) + AI taste-profile narrative
        // All call a plain REST API over HttpClient - no ASP.NET Core hosting types involved, so
        // unlike INotificationBroadcaster these live entirely in this layer.
        services.AddHttpClient<ITmdbService, TmdbService>();          // Movies / TV Shows
        services.AddHttpClient<IRawgService, RawgService>();          // Games
        services.AddHttpClient<IGoogleBooksService, GoogleBooksService>(); // Books
        services.AddHttpClient<IJikanService, JikanService>();        // Anime / Manga
        services.AddHttpClient<IMusicBrainzService, MusicBrainzService>(); // Music
        services.AddHttpClient<IAiNarrativeGenerator, AnthropicNarrativeGenerator>();

        // Development-only sender: logs the reset link via Serilog instead of sending real mail
        // (§12/D-17). There is no production SMTP configuration in scope for this project.
        services.AddScoped<IEmailSender, DevelopmentEmailSender>();

        // ── Phase 12: background service (§10, §26, D-20) ────────────────────────────────
        // One hosted service - PersonaNestBackgroundService - running both documented tasks
        // (taste-profile refresh + nightly media aggregate reconciliation) as independent loops.
        // Singleton by hosting convention; it creates its own DI scope per cycle so it never
        // holds a scoped IUnitOfWork/DbContext across the whole service lifetime.
        services.AddHostedService<PersonaNestBackgroundService>();

        return services;
    }

    /// <summary>
    /// Applies migrations (Development only) and seeds roles and demo accounts.
    /// Exposed here so Web can trigger seeding without referencing Infrastructure.
    /// </summary>
    public static Task InitializeDatabaseAsync(
        this IServiceProvider services,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
        => DbSeeder.SeedAsync(services, isDevelopment, cancellationToken);
}
