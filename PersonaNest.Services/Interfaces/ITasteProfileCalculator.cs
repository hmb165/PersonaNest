using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>
/// Single source of truth for taste-profile computation (§22). <see cref="ComputeAsync"/> is the
/// read-only path <see cref="IProfileService.GetTasteProfileAsync"/> falls back to when no
/// persisted row exists yet (Phase 10). <see cref="RefreshAsync"/> runs the same computation and
/// persists it - only the Phase 12 background service calls that one.
/// </summary>
public interface ITasteProfileCalculator
{
    /// <summary>Computes the taste profile from live Entry/Tag data. Never persists.</summary>
    Task<TasteProfileDto?> ComputeAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes and upserts the persisted TasteProfile row (and its Categories/Tags) for one
    /// user. If the user now has zero entries, any stale persisted profile is removed instead.
    /// </summary>
    /// <returns>True if a profile was computed and persisted, false if there was nothing to persist.</returns>
    Task<bool> RefreshAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates the AI narrative (bonus: AI) for a user's already-persisted TasteProfile, via
    /// <see cref="IAiNarrativeGenerator"/>. No-ops silently if there is no persisted profile, no AI
    /// provider is configured, or the existing narrative is still within its freshness window -
    /// only the Phase 12 background service calls this, right after <see cref="RefreshAsync"/>.
    /// </summary>
    Task RefreshNarrativeAsync(string userId, CancellationToken cancellationToken = default);
}
