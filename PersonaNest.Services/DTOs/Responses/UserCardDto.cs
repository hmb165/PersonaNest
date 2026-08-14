namespace PersonaNest.Services.DTOs.Responses;

/// <summary>A user in a list - search results, follower lists, admin user management.</summary>
public sealed record UserCardDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? ProfilePictureUrl { get; init; }
    public int EntryCount { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>True when the current viewer already follows this user.</summary>
    public bool IsFollowedByViewer { get; init; }

    // ── Admin-only columns; left empty elsewhere.
    public string? Email { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public bool IsBanned { get; init; }
    public string? BanReason { get; init; }
}
