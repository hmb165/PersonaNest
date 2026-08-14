namespace PersonaNest.Domain.Enums;

/// <summary>
/// Moderation state of a community-submitted Media item. New media defaults to
/// <see cref="Approved"/> so the search -> add -> create-entry flow of §4 is never blocked;
/// moderation is post-hoc. Specification v3 §2.2 (decision D-9).
/// </summary>
public enum MediaStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
