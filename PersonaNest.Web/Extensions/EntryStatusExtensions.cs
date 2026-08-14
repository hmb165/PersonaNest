using PersonaNest.Domain.Enums;

namespace PersonaNest.Web.Extensions;

/// <summary>
/// The design system's status badges (§3, decision D-15: "Watching" is retired in favour of a
/// category-neutral "In Progress"). Centralised here because every entry list - My Entries, an
/// entry's own Details page, the community feed on a Media page, and a profile's recent entries -
/// renders the same badge.
/// </summary>
public static class EntryStatusExtensions
{
    public static string ToBadgeClass(this EntryStatus status) => status switch
    {
        EntryStatus.InProgress => "status-watching",
        EntryStatus.Completed => "status-completed",
        EntryStatus.Planning => "status-planning",
        EntryStatus.Dropped => "status-dropped",
        EntryStatus.Paused => "status-paused",
        _ => "badge-muted"
    };

    public static string ToDisplayLabel(this EntryStatus status) =>
        status == EntryStatus.InProgress ? "In Progress" : status.ToString();
}
