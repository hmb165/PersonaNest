using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Responses;

/// <summary>A collection tile. Cover art comes from the first four items.</summary>
public sealed record CollectionCardDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Privacy Privacy { get; init; }
    public int ItemCount { get; init; }

    /// <summary>Up to four official covers, used to build the tile mosaic.</summary>
    public IReadOnlyList<string> PreviewCoverUrls { get; init; } = Array.Empty<string>();

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>A collection with its media, ordered by Position.</summary>
public sealed record CollectionDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Privacy Privacy { get; init; }

    public string OwnerId { get; init; } = string.Empty;
    public string OwnerUserName { get; init; } = string.Empty;
    public string OwnerDisplayName { get; init; } = string.Empty;

    public bool ViewerIsOwner { get; init; }

    public IReadOnlyList<CollectionItemDto> Items { get; init; }
        = Array.Empty<CollectionItemDto>();

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>One media item inside a collection.</summary>
public sealed record CollectionItemDto
{
    public int MediaId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? OfficialCoverUrl { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryColorToken { get; init; } = string.Empty;
    public int? ReleaseYear { get; init; }
    public decimal? AverageRating { get; init; }
    public int Position { get; init; }
    public DateTime AddedAt { get; init; }
}
