using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.ViewModels;

/// <summary>
/// ViewModels compose DTOs into exactly what one page renders (§11). EF entities are never
/// handed to a view.
/// </summary>
public sealed class HomeViewModel
{
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<MediaCardDto> Trending { get; set; } = Array.Empty<MediaCardDto>();
    public IReadOnlyList<EntryCardDto> CommunityActivity { get; set; } = Array.Empty<EntryCardDto>();
}

/// <summary>/Search — media grid plus the category filter pills.</summary>
public sealed class SearchViewModel
{
    public MediaSearchRequest Filter { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<MediaCardDto> Results { get; set; } = Array.Empty<MediaCardDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasResults => Results.Count > 0;
}

/// <summary>/Media/Details/{id} — the shared community page.</summary>
public sealed class MediaDetailsViewModel
{
    public MediaDetailDto Media { get; set; } = new();
    public IReadOnlyList<EntryCardDto> CommunityEntries { get; set; } = Array.Empty<EntryCardDto>();
    public int CommunityEntryTotal { get; set; }
    public int Page { get; set; } = 1;

    /// <summary>The signed-in viewer's own collections, for the "+ Add to Collection" picker (§20).</summary>
    public IReadOnlyList<CollectionCardDto> ViewerCollections { get; set; } = Array.Empty<CollectionCardDto>();
}

/// <summary>/Media/Add — with the duplicate warning §4 requires.</summary>
public sealed class AddMediaViewModel
{
    public CreateMediaRequest Form { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();

    /// <summary>Populated when a first submit found candidates; the user confirms to continue.</summary>
    public IReadOnlyList<MediaCardDto> PossibleDuplicates { get; set; } = Array.Empty<MediaCardDto>();

    public bool HasDuplicateWarning => PossibleDuplicates.Count > 0;
}
