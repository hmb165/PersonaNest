using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>
/// Turns a computed <see cref="TasteProfileDto"/> into a short personalized paragraph via an LLM
/// (bonus requirement: AI). Returns null - never throws - when no API key is configured or the
/// call fails, so a missing/expired key degrades to "no narrative shown" rather than breaking the
/// taste-profile refresh cycle that calls it.
/// </summary>
public interface IAiNarrativeGenerator
{
    Task<string?> GenerateAsync(
        string displayName, TasteProfileDto profile, CancellationToken cancellationToken = default);
}
