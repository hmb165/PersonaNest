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

    /// <summary>Live Google Books hits when the Books filter is active - see SearchController.</summary>
    public IReadOnlyList<ExternalSearchResultDto> ExternalBookResults { get; set; } =
        Array.Empty<ExternalSearchResultDto>();

    /// <summary>Live Kitsu hits when the Anime filter is active - see SearchController.</summary>
    public IReadOnlyList<ExternalSearchResultDto> ExternalAnimeResults { get; set; } =
        Array.Empty<ExternalSearchResultDto>();

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasResults => Results.Count > 0;
}

/// <summary>One "More from &lt;provider&gt;" section on /Search - see Views/Search/_ExternalResults.</summary>
public sealed class ExternalResultsSectionViewModel
{
    public string SourceLabel { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public IReadOnlyList<ExternalSearchResultDto> Results { get; set; } = Array.Empty<ExternalSearchResultDto>();
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

/// <summary>/Media/Edit — Moderator/Admin only, corrects an existing catalogue row's fields.
/// AverageRating is deliberately not here: it's a cached aggregate written through from Entries
/// and reconciled nightly (decision D-20), so hand-editing it would just be overwritten.</summary>
public sealed class EditMediaViewModel
{
    public UpdateMediaRequest Form { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
}
