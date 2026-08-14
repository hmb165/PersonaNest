using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonaNest.Infrastructure;
using PersonaNest.Infrastructure.Seed;
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
        services.AddScoped<IModeratorApplicationService, ModeratorApplicationService>();
        services.AddScoped<IAdminService, AdminService>();

        // Phase 12 registers the taste-profile background service here.

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
