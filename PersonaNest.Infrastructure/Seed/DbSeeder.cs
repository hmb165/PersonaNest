using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PersonaNest.Domain.Entities;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Seed;

/// <summary>
/// Startup seeding (§14).
/// <list type="bullet">
///   <item>Roles are seeded in every environment.</item>
///   <item>Themes, Categories and Tags are seeded through the migration itself (HasData).</item>
///   <item>Demo accounts are seeded <b>only in Development</b>, with passwords read from
///         configuration. Nothing is hard-coded.</item>
/// </list>
/// </summary>
public static class DbSeeder
{
    public const string RoleUser = "User";
    public const string RoleModerator = "Moderator";
    public const string RoleAdmin = "Admin";

    public static readonly string[] AllRoles = { RoleUser, RoleModerator, RoleAdmin };

    private static readonly (string Key, string UserName, string Email, string DisplayName, string Role)[]
        DevAccounts =
        {
            ("Seed:AdminPassword",     "admin",     "admin@personanest.local",     "Site Admin",      RoleAdmin),
            ("Seed:ModeratorPassword", "moderator", "moderator@personanest.local", "Community Mod",   RoleModerator),
            ("Seed:UserPassword",      "demo_user", "demo@personanest.local",      "Demo User",       RoleUser)
        };

    public static async Task SeedAsync(
        IServiceProvider services,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));
        var context = sp.GetRequiredService<PersonaNestDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = sp.GetRequiredService<IConfiguration>();

        // Applying migrations automatically is a development convenience only. In any other
        // environment the deployment pipeline owns schema changes.
        if (isDevelopment)
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        await SeedRolesAsync(roleManager, logger);

        if (isDevelopment)
        {
            await SeedDevelopmentAccountsAsync(userManager, configuration, logger);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        foreach (var role in AllRoles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role '{role}': {Describe(result)}");
            }

            logger.LogInformation("Seeded role {Role}.", role);
        }
    }

    private static async Task SeedDevelopmentAccountsAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        foreach (var (key, userName, email, displayName, role) in DevAccounts)
        {
            if (await userManager.FindByNameAsync(userName) is not null)
            {
                continue;
            }

            // Fail loudly rather than inventing a default. Set these with user-secrets:
            //   dotnet user-secrets set "Seed:AdminPassword" "<password>" -p PersonaNest.Web
            var password = configuration[key];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"Development seeding needs configuration value '{key}'. " +
                    "Set it with: dotnet user-secrets set \"" + key + "\" \"<password>\" " +
                    "-p PersonaNest.Web");
            }

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                ThemeId = 1,
                CreatedAt = DateTime.UtcNow
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create seed user '{userName}': {Describe(created)}");
            }

            var assigned = await userManager.AddToRoleAsync(user, role);
            if (!assigned.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not add '{userName}' to role '{role}': {Describe(assigned)}");
            }

            logger.LogInformation(
                "Seeded development account {UserName} in role {Role}.", userName, role);
        }
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
}
