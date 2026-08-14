using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.ViewModels;

/// <summary>/Profile/{username} — the visual centrepiece.</summary>
public sealed class ProfileViewModel
{
    public ProfileHeaderDto Header { get; set; } = new();
    public ProfileStatsDto Stats { get; set; } = new();
    public TasteProfileDto? TasteProfile { get; set; }
    public IReadOnlyList<EntryCardDto> RecentEntries { get; set; } = Array.Empty<EntryCardDto>();
    public IReadOnlyList<MediaCardDto> Favorites { get; set; } = Array.Empty<MediaCardDto>();
    public IReadOnlyList<CollectionCardDto> Collections { get; set; }
        = Array.Empty<CollectionCardDto>();

    /// <summary>Which profile tab is active: Entries, Collections, Favorites, Taste.</summary>
    public string ActiveTab { get; set; } = "Entries";
}

/// <summary>/Dashboard — personal landing after login (§23).</summary>
public sealed class DashboardViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public ProfileStatsDto Stats { get; set; } = new();
    public TasteProfileDto? TasteProfile { get; set; }
    public IReadOnlyList<EntryCardDto> RecentEntries { get; set; } = Array.Empty<EntryCardDto>();
    public IReadOnlyList<EntryCardDto> FollowingActivity { get; set; } = Array.Empty<EntryCardDto>();
    public int EntriesThisWeek { get; set; }
}

/// <summary>/Settings — profile, appearance and privacy panels on one page.</summary>
public sealed class SettingsViewModel
{
    public UpdateProfileRequest Profile { get; set; } = new();
    public UpdateAppearanceRequest Appearance { get; set; } = new();
    public UpdatePrivacyRequest PrivacySettings { get; set; } = new();
    public IReadOnlyList<ThemeDto> Themes { get; set; } = Array.Empty<ThemeDto>();

    /// <summary>Drives the "Apply to become a Moderator" panel state.</summary>
    public ModeratorApplicationDto? LatestApplication { get; set; }
}

/// <summary>/Social/Followers and /Social/Following — one two-tab view.</summary>
public sealed class FollowListViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool ShowingFollowers { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public IReadOnlyList<UserCardDto> Users { get; set; } = Array.Empty<UserCardDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
}
