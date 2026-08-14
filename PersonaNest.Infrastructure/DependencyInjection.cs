using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Infrastructure.Data;
using PersonaNest.Infrastructure.Repositories;

// The UnitOfWork class sits inside a namespace of the same name, so it is aliased here to keep
// the registration below unambiguous.
using UnitOfWorkImplementation = PersonaNest.Infrastructure.UnitOfWork.UnitOfWork;

namespace PersonaNest.Infrastructure;

/// <summary>
/// Registration entry point for the Infrastructure layer.
/// <para>
/// Called by <c>PersonaNest.Services.DependencyInjection.AddApplicationServices</c>, never by
/// PersonaNest.Web directly - that is what keeps the approved dependency direction
/// Web -&gt; Services -&gt; Infrastructure intact (approved option (a)).
/// </para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<PersonaNestDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(PersonaNestDbContext).Assembly.GetName().Name);

                // Deliberately no EnableRetryOnFailure(): EF Core's retrying execution strategy
                // refuses to run inside a user-initiated transaction ("does not support
                // user-initiated transactions"), and IUnitOfWork.BeginTransactionAsync (§9,
                // required) is exactly that. The two are mutually exclusive; the Unit of Work is
                // the required piece, so the transient-fault retry wrapper loses. Found when
                // EntryService.CreateAsync (Phase 7) became the first caller to actually open a
                // transaction.
            }));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // §14 - password security.
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                // Required for the admin Ban action, which sets LockoutEnd (decision D-9).
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<PersonaNestDbContext>()
            .AddDefaultTokenProviders();

        // Routes match the approved navigation map in the design system.
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Auth/Login";
            options.LogoutPath = "/Auth/Logout";
            options.AccessDeniedPath = "/Auth/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
        });

        // ── Phase 3: repositories and Unit of Work (§8, §9) ──────────────────────────────
        // Scoped, so one DbContext, one change tracker and one unit of work per request.
        //
        // The open generic registration lets any entity without a specific repository be reached
        // through IUnitOfWork.Repository<T>(). Services depend on IUnitOfWork and the repository
        // interfaces only - never on PersonaNestDbContext (§8, rule 4).
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IEntryRepository, EntryRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWorkImplementation>();

        return services;
    }
}
