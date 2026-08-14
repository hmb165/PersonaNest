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
    /// Resolved accent: the user's custom hex when set, otherwise their theme's, otherwise the
    /// default theme's. Rendered as an inline <c>--primary</c> on the profile container.
    /// </summary>
    public string AccentColor { get; init; } = string.Empty;
    public string AccentColorDark { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public bool IsViewerSelf { get; init; }
    public bool IsFollowedByViewer { get; init; }
}
