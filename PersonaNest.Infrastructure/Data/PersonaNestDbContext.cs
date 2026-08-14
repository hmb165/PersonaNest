using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data;

/// <summary>
/// The PersonaNest database context. Inherits <see cref="IdentityDbContext{TUser}"/> so the
/// AspNetUsers / AspNetRoles / AspNetUserRoles tables come from the framework (§6, §14).
/// <para>
/// All entity, relationship, key, index, constraint and delete-behaviour configuration is done
/// with the EF Core <b>Fluent API</b> in <c>Data/Configurations/</c>. Data annotations are used
/// only for input validation on DTOs and ViewModels.
/// </para>
/// </summary>
public class PersonaNestDbContext : IdentityDbContext<ApplicationUser>
{
    public PersonaNestDbContext(DbContextOptions<PersonaNestDbContext> options)
        : base(options)
    {
    }

    // ── Catalogue
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Media> Media => Set<Media>();

    // ── Journal
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EntryTag> EntryTags => Set<EntryTag>();

    // ── Social
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<EntryLike> EntryLikes => Set<EntryLike>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    // ── Curation
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionItem> CollectionItems => Set<CollectionItem>();

    // ── Personalisation
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<TasteProfile> TasteProfiles => Set<TasteProfile>();
    public DbSet<TasteProfileCategory> TasteProfileCategories => Set<TasteProfileCategory>();
    public DbSet<TasteProfileTag> TasteProfileTags => Set<TasteProfileTag>();

    // ── Moderation. Three separate tables, each with a real FK to its target (decision D-4).
    public DbSet<ModeratorApplication> ModeratorApplications => Set<ModeratorApplication>();
    public DbSet<MediaReport> MediaReports => Set<MediaReport>();
    public DbSet<EntryReport> EntryReports => Set<EntryReport>();
    public DbSet<CommentReport> CommentReports => Set<CommentReport>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity's own model first.
        base.OnModelCreating(builder);

        // Then every IEntityTypeConfiguration<T> in this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(PersonaNestDbContext).Assembly);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  GLOBAL QUERY FILTERS
    //
    //  Applied to Media, Entry and Comment via their configurations:  !IsDeleted
    //  Moderator and admin paths opt back in with .IgnoreQueryFilters().
    //
    //  ApplicationUser deliberately has NO filter, which is a departure from
    //  Specification v3 §5 and is flagged in the Phase 2 report for your ruling.
    //
    //  Reasoning: account deletion anonymises in place rather than destroying rows
    //  (decision D-17), because Restrict on User -> Entry makes hard deletion impossible.
    //  An anonymised user must therefore still JOIN - their entries and comments stay visible
    //  attributed to "[deleted]". A global filter on ApplicationUser would make every required
    //  Entry.User / Comment.User navigation resolve to null for those rows and throw at render
    //  time. Excluding anonymised users from search and follow suggestions is a query-level
    //  concern, handled in the relevant repositories in Phase 3.
    //
    //  To revert to the letter of the specification, add to ApplicationUserConfiguration:
    //      builder.HasQueryFilter(u => !u.IsDeleted);
    // ═══════════════════════════════════════════════════════════════════════════════════
}
