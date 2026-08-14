using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Entities;

/// <summary>
/// A user's request for the Moderator role (§7). Reviewed by an Admin; approval assigns the
/// Identity role. A filtered unique index allows only one Pending application per user.
/// </summary>
public class ModeratorApplication
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>"Why do you want to moderate PersonaNest?"</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>"Relevant experience (optional)" — the second textarea on the design's form.</summary>
    public string? RelevantExperience { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime AppliedAt { get; set; }

    /// <summary>Admins review applications; moderators review reports.</summary>
    public string? ReviewedByAdminId { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }
}
