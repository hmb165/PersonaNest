using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.ViewModels;

/// <summary>/Entries — the filterable My Entries table.</summary>
public sealed class MyEntriesViewModel
{
    public MyEntriesRequest Filter { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<EntrySummaryDto> Entries { get; set; } = Array.Empty<EntrySummaryDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// /Entries/Create and /Entries/Edit share one form. <see cref="IsEdit"/> switches the heading,
/// the action and whether the media picker is locked.
/// </summary>
public sealed class EntryFormViewModel
{
    public bool IsEdit { get; set; }
    public int? EntryId { get; set; }

    public CreateEntryRequest Create { get; set; } = new();
    public UpdateEntryRequest Edit { get; set; } = new();

    /// <summary>The media being logged, shown in the sidebar preview.</summary>
    public MediaCardDto? Media { get; set; }

    public IReadOnlyList<TagDto> AvailableTags { get; set; } = Array.Empty<TagDto>();
    public IReadOnlyList<int> SelectedTagIds { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Set when the user already has an entry for this media, so the page can offer the edit
    /// link instead of failing on the unique index (decision D-11).
    /// </summary>
    public int? ExistingEntryId { get; set; }
}

/// <summary>/Entries/Details/{id} — full entry with the comment thread.</summary>
public sealed class EntryDetailsViewModel
{
    public EntryDetailDto Entry { get; set; } = new();
    public bool ViewerCanComment { get; set; }

    /// <summary>Top-level comments, each with its replies attached (§5, §18).</summary>
    public IReadOnlyList<CommentDto> Comments { get; set; } = Array.Empty<CommentDto>();

    /// <summary>The signed-in viewer's own collections, for the "+ Add to Collection" picker (§20).</summary>
    public IReadOnlyList<CollectionCardDto> ViewerCollections { get; set; } = Array.Empty<CollectionCardDto>();
}
