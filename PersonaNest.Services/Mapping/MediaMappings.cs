using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>Manual Mapping for <see cref="Media"/>.</summary>
public static class MediaMappings
{
    /// <summary>Grid/poster projection - home, search, favourites.</summary>
    public static Expression<Func<Media, MediaCardDto>> ToCardDto => m => new MediaCardDto
    {
        Id = m.Id,
        Title = m.Title,
        OfficialCoverUrl = m.OfficialCoverUrl,
        Creator = m.Creator,
        ReleaseYear = m.ReleaseYear,
        CategoryName = m.Category.Name,
        CategoryColorToken = m.Category.ColorToken,
        AverageRating = m.AverageRating,
        EntryCount = m.EntryCount
    };

    /// <summary>
    /// Detail -&gt; card reshape, for pages that already loaded the detail DTO and just need the
    /// smaller shape for a sidebar preview (the Create/Edit Entry form).
    /// </summary>
    public static MediaCardDto AsCardDto(this MediaDetailDto detail) => new()
    {
        Id = detail.Id,
        Title = detail.Title,
        OfficialCoverUrl = detail.OfficialCoverUrl,
        Creator = detail.Creator,
        ReleaseYear = detail.ReleaseYear,
        CategoryName = detail.CategoryName,
        CategoryColorToken = detail.CategoryColorToken,
        AverageRating = detail.AverageRating,
        EntryCount = detail.EntryCount
    };

    /// <summary>
    /// Media details. Parameterised by viewer, because two fields are viewer-relative; pass null
    /// for an anonymous visitor.
    /// </summary>
    public static Expression<Func<Media, MediaDetailDto>> ToDetailDto(string? viewerId) =>
        m => new MediaDetailDto
        {
            Id = m.Id,
            Title = m.Title,
            Description = m.Description,
            OfficialCoverUrl = m.OfficialCoverUrl,
            Creator = m.Creator,
            ReleaseYear = m.ReleaseYear,
            RuntimeMinutes = m.RuntimeMinutes,
            CategoryId = m.CategoryId,
            CategoryName = m.Category.Name,
            CategoryColorToken = m.Category.ColorToken,
            AverageRating = m.AverageRating,
            RatingCount = m.RatingCount,
            EntryCount = m.EntryCount,
            AddedByUserName = m.CreatedBy.UserName!,
            CreatedAt = m.CreatedAt,
            IsFavoritedByViewer =
                viewerId != null && m.Favorites.Any(f => f.UserId == viewerId),
            ViewerEntryId = viewerId == null
                ? null
                : m.Entries.Where(e => e.UserId == viewerId)
                           .Select(e => (int?)e.Id)
                           .FirstOrDefault()
        };

    /// <summary>Request -&gt; new entity. Server owns every field the client must not set.</summary>
    public static Media ToEntity(this CreateMediaRequest request, string createdById, DateTime utcNow)
        => new()
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null : request.Description.Trim(),
            OfficialCoverUrl = string.IsNullOrWhiteSpace(request.OfficialCoverUrl)
                ? null : request.OfficialCoverUrl.Trim(),
            Creator = string.IsNullOrWhiteSpace(request.Creator)
                ? null : request.Creator.Trim(),
            ReleaseYear = request.ReleaseYear,
            CategoryId = request.CategoryId,
            CreatedById = createdById,
            CreatedAt = utcNow
        };

    /// <summary>Applies an edit to a tracked entity.</summary>
    public static void ApplyTo(this UpdateMediaRequest request, Media media, DateTime utcNow)
    {
        media.Title = request.Title.Trim();
        media.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null : request.Description.Trim();
        media.OfficialCoverUrl = string.IsNullOrWhiteSpace(request.OfficialCoverUrl)
            ? null : request.OfficialCoverUrl.Trim();
        media.Creator = string.IsNullOrWhiteSpace(request.Creator)
            ? null : request.Creator.Trim();
        media.ReleaseYear = request.ReleaseYear;
        media.CategoryId = request.CategoryId;
        media.UpdatedAt = utcNow;
    }
}
