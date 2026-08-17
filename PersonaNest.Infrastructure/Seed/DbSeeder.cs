using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonaNest.Domain.Constants;
using PersonaNest.Domain.Entities;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Seed;

/// <summary>
/// Startup seeding (§14).
/// <list type="bullet">
///   <item>Roles are seeded in every environment.</item>
///   <item>Themes, Categories and Tags are seeded through the migration itself (HasData).</item>
///   <item>Demo accounts are seeded <b>only in Development</b>. A password from
///         <c>dotnet user-secrets</c> is preferred; if none is configured (e.g. a fresh clone that
///         hasn't been set up yet), a random one is generated instead so the app never crashes on
///         first run. Generated passwords are logged once and written to a gitignored local file -
///         never a hard-coded value, and never committed.</item>
///   <item>A starter media catalogue is seeded <b>only in Development</b>, attributed to the
///         seeded admin account, so the trending rail and category pages aren't empty locally.</item>
/// </list>
/// </summary>
public static class DbSeeder
{
    // Role names live in PersonaNest.Domain.Constants.Roles so the seeder, the services and the
    // [Authorize(Roles = ...)] attributes all name the same strings.
    public static readonly string[] AllRoles = Roles.All;

    private static readonly (string Key, string UserName, string Email, string DisplayName, string Role)[]
        DevAccounts =
        {
            ("Seed:AdminPassword",     "admin",     "admin@personanest.local",     "Site Admin",      Roles.Admin),
            ("Seed:ModeratorPassword", "moderator", "moderator@personanest.local", "Community Mod",   Roles.Moderator),
            ("Seed:UserPassword",      "user",      "user@personanest.local",      "Demo User",        Roles.User)
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
        var environment = sp.GetRequiredService<IHostEnvironment>();

        // Applying migrations automatically is a development convenience only. In any other
        // environment the deployment pipeline owns schema changes.
        if (isDevelopment)
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        await SeedRolesAsync(roleManager, logger);

        if (isDevelopment)
        {
            await SeedDevelopmentAccountsAsync(
                userManager, configuration, environment.ContentRootPath, logger);
            await SeedCatalogueAsync(context, userManager, logger, cancellationToken);
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
        string contentRootPath,
        ILogger logger)
    {
        var generated = new List<(string UserName, string Password)>();

        foreach (var (key, userName, email, displayName, role) in DevAccounts)
        {
            if (await userManager.FindByNameAsync(userName) is not null)
            {
                continue;
            }

            // A user-secret is preferred; a fresh clone won't have one (passwords are never
            // committed), so a random one is generated instead rather than crashing the whole app
            // on first run. Override at any time with:
            //   dotnet user-secrets set "Seed:AdminPassword" "<password>" -p PersonaNest.Web
            var password = configuration[key];
            if (string.IsNullOrWhiteSpace(password))
            {
                password = GenerateDevPassword();
                generated.Add((userName, password));
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

        if (generated.Count > 0)
        {
            WriteGeneratedCredentials(generated, contentRootPath, logger);
        }
    }

    /// <summary>
    /// Meets Identity's configured policy (8+ chars, upper/lower/digit - see
    /// DependencyInjection.AddIdentity) by construction, not by chance: the first three
    /// characters each come from a required class, the rest are drawn from the combined pool.
    /// Ambiguous-looking characters (0/O, 1/I/l) are excluded since this gets read off a screen.
    /// </summary>
    private static string GenerateDevPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = upper + lower + digits;

        var chars = new char[12];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        for (var i = 3; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        return new string(chars);
    }

    /// <summary>
    /// Generated passwords are logged once (easy to miss in a wall of startup output) and also
    /// written to a local, gitignored file so they can be found again after the console scrolls
    /// away - this file is never committed and is overwritten fresh on every generation.
    /// </summary>
    private static void WriteGeneratedCredentials(
        List<(string UserName, string Password)> generated, string contentRootPath, ILogger logger)
    {
        var path = Path.Combine(contentRootPath, "dev-credentials.local.txt");
        var lines = new List<string>
        {
            "PersonaNest - auto-generated development credentials",
            "",
            "No Seed:<Role>Password was configured via dotnet user-secrets, so these were",
            "generated for you on first run. Existing accounts keep their original password -",
            "this file only reflects whichever accounts were most recently created this way.",
            ""
        };
        lines.AddRange(generated.Select(g => $"  {g.UserName} / {g.Password}"));
        lines.Add("");
        lines.Add("To set your own instead: dotnet user-secrets set \"Seed:AdminPassword\" \"<password>\" -p PersonaNest.Web");

        File.WriteAllLines(path, lines);

        logger.LogWarning(
            "No Seed:<Role>Password configured - generated credentials for {Count} account(s). " +
            "Logged below and saved to {Path}:\n{Credentials}",
            generated.Count,
            path,
            string.Join('\n', generated.Select(g => $"  {g.UserName} / {g.Password}")));
    }

    // Matches CategoryConfiguration's HasData ids.
    private const int CategoryGames = 1;
    private const int CategoryMovies = 2;
    private const int CategoryTvShows = 3;
    private const int CategoryAnime = 4;
    private const int CategoryManga = 5;
    private const int CategoryBooks = 6;
    private const int CategoryMusic = 7;

    private static async Task SeedCatalogueAsync(
        PersonaNestDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var admin = await userManager.FindByNameAsync("admin");
        if (admin is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        Media M(int categoryId, string title, string creator, int year, string description) => new()
        {
            Title = title,
            Creator = creator,
            ReleaseYear = year,
            Description = description,
            CategoryId = categoryId,
            CreatedById = admin.Id,
            CreatedAt = now
        };

        var items = new[]
        {
            // Games
            M(CategoryGames, "Elden Ring", "FromSoftware", 2022, "An open-world action RPG set in the Lands Between, created with George R. R. Martin."),
            M(CategoryGames, "The Legend of Zelda: Breath of the Wild", "Nintendo", 2017, "An open-world reinvention of the Zelda series set in a ruined kingdom of Hyrule."),
            M(CategoryGames, "God of War Ragnarök", "Santa Monica Studio", 2022, "Kratos and Atreus journey through the Nine Realms as Fimbulwinter approaches."),
            M(CategoryGames, "The Witcher 3: Wild Hunt", "CD Projekt Red", 2015, "Monster hunter Geralt of Rivia searches for his adopted daughter across a war-torn continent."),
            M(CategoryGames, "Red Dead Redemption 2", "Rockstar Games", 2018, "An epic tale of outlaw life in America at the dawn of the modern age."),
            M(CategoryGames, "Hollow Knight", "Team Cherry", 2017, "A hand-drawn metroidvania through the ruined insect kingdom of Hallownest."),
            M(CategoryGames, "Baldur's Gate 3", "Larian Studios", 2023, "A party-based RPG set in the Forgotten Realms, built on Dungeons & Dragons rules."),
            M(CategoryGames, "Hades", "Supergiant Games", 2020, "A roguelike dungeon crawler starring Zagreus, the son of Hades, escaping the Underworld."),

            // Movies
            M(CategoryMovies, "The Godfather", "Francis Ford Coppola", 1972, "The aging patriarch of an organized crime dynasty transfers control to his reluctant son."),
            M(CategoryMovies, "Parasite", "Bong Joon-ho", 2019, "Greed and class discrimination threaten the newly formed symbiotic relationship between a poor family and a wealthy one."),
            M(CategoryMovies, "Inception", "Christopher Nolan", 2010, "A thief who steals corporate secrets through dream-sharing technology is given a chance to erase his past crimes."),
            M(CategoryMovies, "Pulp Fiction", "Quentin Tarantino", 1994, "The lives of two mob hitmen, a boxer, and a gangster's wife intertwine in four tales of violence."),
            M(CategoryMovies, "The Dark Knight", "Christopher Nolan", 2008, "Batman faces the Joker, a criminal mastermind who plunges Gotham into anarchy."),
            M(CategoryMovies, "Everything Everywhere All at Once", "Daniel Kwan & Daniel Scheinert", 2022, "An aging Chinese immigrant is swept into an adventure connecting her to parallel universes."),
            M(CategoryMovies, "La La Land", "Damien Chazelle", 2016, "A jazz pianist and an aspiring actress fall in love while pursuing their dreams in Los Angeles."),

            // TV Shows
            M(CategoryTvShows, "Breaking Bad", "Vince Gilligan", 2008, "A chemistry teacher turned methamphetamine manufacturer navigates the drug trade."),
            M(CategoryTvShows, "The Wire", "David Simon", 2002, "A layered look at Baltimore through the drug trade, the police, and the institutions around them."),
            M(CategoryTvShows, "Succession", "Jesse Armstrong", 2018, "The Roy family battles for control of their global media and entertainment conglomerate."),
            M(CategoryTvShows, "Chernobyl", "Craig Mazin", 2019, "A dramatization of the 1986 nuclear disaster and the sacrifices made to save Europe."),
            M(CategoryTvShows, "The Sopranos", "David Chase", 1999, "New Jersey mob boss Tony Soprano balances family life with organized crime, in therapy."),
            M(CategoryTvShows, "Stranger Things", "The Duffer Brothers", 2016, "Kids in a small town uncover a government conspiracy and a monster from another dimension."),
            M(CategoryTvShows, "Fleabag", "Phoebe Waller-Bridge", 2016, "A grief-stricken, sharp-tongued woman navigates life and relationships in London."),

            // Anime
            M(CategoryAnime, "Spirited Away", "Studio Ghibli / Hayao Miyazaki", 2001, "A girl wandering into a world ruled by gods and witches must work in a bathhouse to free her parents."),
            M(CategoryAnime, "Fullmetal Alchemist: Brotherhood", "Studio Bones", 2009, "Two brothers use alchemy in a quest to restore their bodies after a forbidden ritual goes wrong."),
            M(CategoryAnime, "Attack on Titan", "Studio Wit / MAPPA", 2013, "Humanity fights for survival against man-eating Titans from behind massive walls."),
            M(CategoryAnime, "Your Name", "Makoto Shinkai", 2016, "Two teenagers discover they are mysteriously swapping bodies across distance and time."),
            M(CategoryAnime, "Cowboy Bebop", "Sunrise", 1998, "A ragtag crew of bounty hunters chase criminals across the solar system."),
            M(CategoryAnime, "Death Note", "Madhouse", 2006, "A student gains a notebook that can kill anyone whose name is written in it."),
            M(CategoryAnime, "Princess Mononoke", "Studio Ghibli / Hayao Miyazaki", 1997, "A prince becomes entangled in a struggle between forest gods and a mining colony."),
            M(CategoryAnime, "Naruto: Shippuuden", "Studio Pierrot", 2007, "It has been two and a half years since Naruto Uzumaki left the Hidden Leaf Village for training. Now Akatsuki, a mysterious organization of elite rogue ninja, threatens the shinobi world he has sworn to protect."),
            M(CategoryAnime, "Fire Force", "Atsushi Ohkubo", 2015, "In a world ravaged by spontaneous human combustion, a special company of pyrokinetic firefighters battles the deadly phenomenon consuming the last city standing."),

            // Manga
            M(CategoryManga, "Berserk", "Kentaro Miura", 1989, "A lone mercenary swordsman is pursued by demonic forces in a brutal medieval-inspired world."),
            M(CategoryManga, "One Piece", "Eiichiro Oda", 1997, "A young pirate sets sail to find the legendary treasure known as One Piece."),
            M(CategoryManga, "Vagabond", "Takehiko Inoue", 1998, "A fictionalized account of the life of legendary swordsman Miyamoto Musashi."),
            M(CategoryManga, "Fullmetal Alchemist", "Hiromu Arakawa", 2001, "Two brothers use alchemy in a quest to restore their bodies after a forbidden ritual goes wrong."),
            M(CategoryManga, "Chainsaw Man", "Tatsuki Fujimoto", 2018, "A young devil hunter merges with his chainsaw devil to survive Japan's criminal underworld."),
            M(CategoryManga, "Vinland Saga", "Makoto Yukimura", 2005, "A young Viking's quest for revenge unfolds against the historical backdrop of medieval Europe."),
            M(CategoryManga, "Jujutsu Kaisen", "Gege Akutami", 2018, "A boy swallows a cursed talisman to save his friends and is drawn into a secret academy for sorcerers, hunting the deadly curses roaming Japan."),

            // Books
            M(CategoryBooks, "Dune", "Frank Herbert", 1965, "A young heir becomes entangled in the politics and mysticism of the desert planet Arrakis."),
            M(CategoryBooks, "1984", "George Orwell", 1949, "A dystopian vision of a totalitarian future under the constant surveillance of Big Brother."),
            M(CategoryBooks, "The Hobbit", "J.R.R. Tolkien", 1937, "A reluctant hobbit joins a company of dwarves on a quest to reclaim their mountain home."),
            M(CategoryBooks, "Crime and Punishment", "Fyodor Dostoevsky", 1866, "A destitute former student commits murder and grapples with the moral consequences."),
            M(CategoryBooks, "The Name of the Wind", "Patrick Rothfuss", 2007, "A legendary figure recounts the true story of his life as a magician, thief, and musician."),
            M(CategoryBooks, "Project Hail Mary", "Andy Weir", 2021, "A lone astronaut wakes with no memory on a mission to save humanity from extinction."),
            M(CategoryBooks, "Norwegian Wood", "Haruki Murakami", 1987, "A man recalls his youth and the two women who shaped his life in 1960s Tokyo."),
            M(CategoryBooks, "White Nights", "Fyodor Dostoevsky", 1848, "A lonely dreamer in St. Petersburg spends four nights sharing his hopes and heartbreak with a young woman waiting for a man who may never return."),
            M(CategoryBooks, "Harry Potter and the Goblet of Fire", "J.K. Rowling", 2000, "Harry is astonished to find his name entered into the deadly Triwizard Tournament, facing dragons, dark wizards, and challenges no fourth-year student should have to survive."),

            // Music
            M(CategoryMusic, "The Dark Side of the Moon", "Pink Floyd", 1973, "A concept album exploring conflict, greed, time, and mental illness."),
            M(CategoryMusic, "To Pimp a Butterfly", "Kendrick Lamar", 2015, "A dense, jazz-inflected exploration of race, fame, and self-worth."),
            M(CategoryMusic, "Rumours", "Fleetwood Mac", 1977, "A landmark rock album written amid the band's own tangled relationships."),
            M(CategoryMusic, "OK Computer", "Radiohead", 1997, "An atmospheric album grappling with alienation and technology at the turn of the millennium."),
            M(CategoryMusic, "Discovery", "Daft Punk", 2001, "A French house album blending disco, funk, and pop into a genre-defining sound."),
            M(CategoryMusic, "Blonde", "Frank Ocean", 2016, "An introspective, genre-blurring album about memory, identity, and love."),
            M(CategoryMusic, "Donda", "Ye (Kanye West)", 2021, "A sprawling, gospel-inflected album named for and dedicated to Kanye West's late mother.")
        };

        // Per-item idempotent (title + category), not "table is empty": a dev database that
        // already has a few user-submitted or test rows should still get topped up with whatever
        // starter titles are missing, rather than being skipped entirely.
        var existing = await context.Media.IgnoreQueryFilters()
            .Select(m => new { m.Title, m.CategoryId })
            .ToListAsync(cancellationToken);
        var existingSet = existing
            .Select(m => (m.Title, m.CategoryId))
            .ToHashSet();

        var missing = items.Where(m => !existingSet.Contains((m.Title, m.CategoryId))).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        context.Media.AddRange(missing);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} starter catalogue items.", missing.Count);
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
}
