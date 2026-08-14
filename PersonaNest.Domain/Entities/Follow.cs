namespace PersonaNest.Domain.Entities;

/// <summary>
/// A one-way follow. Unique on (FollowerId, FollowingId) with a check constraint preventing
/// self-follow. There is no approval state — following is instant (§18).
/// </summary>
public class Follow
{
    public int Id { get; set; }

    /// <summary>The user doing the following.</summary>
    public string FollowerId { get; set; } = string.Empty;
    public ApplicationUser Follower { get; set; } = null!;

    /// <summary>The user being followed.</summary>
    public string FollowingId { get; set; } = string.Empty;
    public ApplicationUser FollowingUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
