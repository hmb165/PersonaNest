namespace PersonaNest.Domain.Entities;

/// <summary>
/// Pre-computed taste summary for one user (§22). Written only by the background service
/// (§26) so the profile and dashboard never recalculate on request. Shares its primary key
/// with <see cref="ApplicationUser"/> (one-to-one).
/// </summary>
public class TasteProfile
{
    /// <summary>Primary key and foreign key to ApplicationUser.</summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public decimal? AverageRating { get; set; }
    public int TotalEntries { get; set; }
    public int TotalReviews { get; set; }

    /// <summary>First day of the user's most active month, UTC.</summary>
    public DateTime? MostActiveMonth { get; set; }

    public DateTime ComputedAt { get; set; }

    /// <summary>AI-generated personalized summary (bonus: AI). Null when no AI provider is
    /// configured, or generation hasn't run yet.</summary>
    public string? AiNarrative { get; set; }
    public DateTime? AiNarrativeGeneratedAt { get; set; }

    public ICollection<TasteProfileCategory> Categories { get; set; }
        = new List<TasteProfileCategory>();
    public ICollection<TasteProfileTag> Tags { get; set; } = new List<TasteProfileTag>();
}
