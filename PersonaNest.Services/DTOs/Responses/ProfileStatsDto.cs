namespace PersonaNest.Services.DTOs.Responses;

/// <summary>The stat strip under a profile header, and the dashboard tiles.</summary>
public sealed record ProfileStatsDto
{
    public int EntryCount { get; init; }
    public int ReviewCount { get; init; }
    public int FollowerCount { get; init; }
    public int FollowingCount { get; init; }
    public int CollectionCount { get; init; }
    public int FavoriteCount { get; init; }
    public decimal? AverageRating { get; init; }
}
