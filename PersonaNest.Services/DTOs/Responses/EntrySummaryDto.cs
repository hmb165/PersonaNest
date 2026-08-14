using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Responses;

/// <summary>A row in the My Entries table - denser than a card, no review text.</summary>
public sealed record EntrySummaryDto
{
    public int Id { get; init; }
    public int MediaId { get; init; }
    public string MediaTitle { get; init; } = string.Empty;
    public string? CoverUrl { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryColorToken { get; init; } = string.Empty;
    public decimal? Rating { get; init; }
    public EntryStatus Status { get; init; }
    public Privacy Privacy { get; init; }
    public DateTime? ConsumedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
