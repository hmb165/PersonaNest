using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.ViewModels;

/// <summary>/Collections — the owner's or a visitor's view of someone's collections.</summary>
public sealed class CollectionsViewModel
{
    public string OwnerUserName { get; set; } = string.Empty;
    public bool ViewerIsOwner { get; set; }
    public IReadOnlyList<CollectionCardDto> Collections { get; set; }
        = Array.Empty<CollectionCardDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;

    /// <summary>Bound by the inline "New Collection" form.</summary>
    public CreateCollectionRequest NewCollection { get; set; } = new();
}

/// <summary>/Collections/Details/{id}.</summary>
public sealed class CollectionDetailsViewModel
{
    public CollectionDetailDto Collection { get; set; } = new();
    public UpdateCollectionRequest EditForm { get; set; } = new();
}

/// <summary>/Favorites (§19).</summary>
public sealed class FavoritesViewModel
{
    public IReadOnlyList<MediaCardDto> Favorites { get; set; } = Array.Empty<MediaCardDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
