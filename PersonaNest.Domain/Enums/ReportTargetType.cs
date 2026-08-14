namespace PersonaNest.Domain.Enums;

/// <summary>
/// Discriminates rows in the unified moderation queue. Reports live in three separate tables
/// with real foreign keys (decision D-4); this enum is projected into
/// <c>ReportQueueItemDto</c> so one queue can mix them and so the Resolve/Dismiss routes can
/// carry a <c>{targetType}</c> segment. NOT PERSISTED. Specification v3 §3 and §6.
/// </summary>
public enum ReportTargetType
{
    Media = 0,
    Entry = 1,
    Comment = 2
}
