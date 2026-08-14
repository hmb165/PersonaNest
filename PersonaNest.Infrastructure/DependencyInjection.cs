using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonaNest.Domain.Entities;
using PersonaNest.Infrastructure.Data;

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
                sql.EnableRetryOnFailure();
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

        // Phase 3 registers the repositories and the Unit of Work here.

        return services;
    }
}
