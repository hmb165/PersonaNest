using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Entities;

/// <summary>
/// A report against a catalogue entry, e.g. a duplicate or incorrect Media row.
/// <para>
/// Reports are three separate tables rather than one table-per-hierarchy, so each keeps a real
/// required foreign key to its target (decision D-4). The single moderation queue the design
/// shows is assembled in the service layer as <c>ReportQueueItemDto</c>.
/// </para>
/// </summary>
public class MediaReport
{
    public int Id { get; set; }

    public string ReporterId { get; set; } = string.Empty;
    public ApplicationUser Reporter { get; set; } = null!;

    public int MediaId { get; set; }
    public Media Media { get; set; } = null!;

    public ReportReason Reason { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Set null if the reviewing moderator's account is later removed, so the
    /// audit record survives.</summary>
    public string? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }

    /// <summary>Why the report was resolved or dismissed.</summary>
    public string? ResolutionNotes { get; set; }
}
