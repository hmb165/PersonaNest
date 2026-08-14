using Microsoft.AspNetCore.Identity;
using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Entities;

/// <summary>
/// The single user record. Roles (User / Moderator / Admin) are ASP.NET Core Identity roles,
/// not separate entities (§6). Key type is <see cref="string"/> — the IdentityUser default
/// (decision D-1).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? BannerUrl { get; set; }

    /// <summary>Custom accent hex (#rrggbb) overriding the selected <see cref="Theme"/>.</summary>
    public string? AccentColor { get; set; }

    public int? ThemeId { get; set; }
    public Theme? Theme { get; set; }

    /// <summary>Privacy applied to newly created Entries. Settings -> Privacy (decision D-17).</summary>
    public Privacy DefaultEntryPrivacy { get; set; } = Privacy.Public;

    /// <summary>
    /// Display-only reason shown in the admin UI. Ban enforcement itself uses Identity's
    /// <c>LockoutEnabled</c> / <c>LockoutEnd</c>, not a parallel flag (decision D-9).
    /// </summary>
    public string? BanReason { get; set; }

    /// <summary>
    /// Marks an anonymised account. There is deliberately NO global query filter on
    /// ApplicationUser — see PersonaNestDbContext for the reasoning.
    /// </summary>
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Inverse navigations are declared only where a query needs them (Specification v3 §2.3).
    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Follow> Following { get; set; } = new List<Follow>();
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public TasteProfile? TasteProfile { get; set; }
}
