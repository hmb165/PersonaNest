using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Responses;

/// <summary>Banner, avatar, bio and accent colour for a profile page.</summary>
public sealed record ProfileHeaderDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public string? BannerUrl { get; init; }

    /// <summary>
    /// The profile's background wash colour, from the user's selected Theme (falling back to the
    /// default theme if none is set). Independent of <see cref="AccentColor"/> - picking a theme
    /// never touches text colour, and picking a custom accent never touches the background.
    /// </summary>
    public string BackgroundColor { get; init; } = string.Empty;

    /// <summary>
    /// The user's custom text-accent hex (falling back to the design default), used only to colour
    /// a handful of highlighted numbers/tabs on their own profile - never the background, never a
    /// button. See <see cref="BackgroundColor"/> for what actually controls the background.
    /// </summary>
    public string AccentColor { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    /// <summary>The account's saved default privacy for new entries (§16's Privacy settings tab).</summary>
    public Privacy DefaultEntryPrivacy { get; init; }

    public bool IsViewerSelf { get; init; }
    public bool IsFollowedByViewer { get; init; }
}

/// <summary>
/// Raw appearance state for the edit form: which Theme is selected (controls the profile
/// background) and the raw stored custom accent hex, if any (controls a few text highlights).
/// </summary>
public sealed record AppearanceDto
{
    public int? ThemeId { get; init; }
    public string? AccentColor { get; init; }
}
