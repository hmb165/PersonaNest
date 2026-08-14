using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.ViewModels;

/// <summary>/Social/Following and /Social/Followers - the same shape, one list flipped for the other (§18).</summary>
public sealed class SocialListViewModel
{
    public string ProfileUserName { get; set; } = string.Empty;
    public string ProfileDisplayName { get; set; } = string.Empty;

    /// <summary>"Following" or "Followers" - which tab is active and which list is shown.</summary>
    public string Mode { get; set; } = "Following";

    public int FollowingCount { get; set; }
    public int FollowerCount { get; set; }

    public IReadOnlyList<UserCardDto> Users { get; set; } = Array.Empty<UserCardDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
