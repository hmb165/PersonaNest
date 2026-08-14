using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>Manual Mapping for <see cref="Entry"/>.</summary>
public static class EntryMappings
{
    /// <summary>
    /// Feed card. Note <c>CoverUrl</c>: the personal cover wins when present, otherwise the
    /// shared official cover - and the official cover is never overwritten (§5).
    /// </summary>
    public static Expression<Func<Entry, EntryCardDto>> ToCardDto => e => new EntryCardDto
    {
        Id = e.Id,
        MediaId = e.MediaId,
        MediaTitle = e.Media.Title,
        CoverUrl = e.PersonalCoverUrl ?? e.Media.OfficialCoverUrl,
        CategoryName = e.Media.Category.Name,
        CategoryColorToken = e.Media.Category.ColorToken,
        Rating = e.Rating,
        Review = e.Review,
        Status = e.Status,
        Privacy = e.Privacy,
        AuthorId = e.UserId,
        AuthorUserName = e.User.UserName!,
        AuthorDisplayName = e.User.DisplayName,
        AuthorProfilePictureUrl = e.User.ProfilePictureUrl,
        LikeCount = e.Likes.Count(),
        CommentCount = e.Comments.Count(),
        CreatedAt = e.CreatedAt
    };

    /// <summary>Dense row for the My Entries table.</summary>
    public static Expression<Func<Entry, EntrySummaryDto>> ToSummaryDto => e => new EntrySummaryDto
    {
        Id = e.Id,
        MediaId = e.MediaId,
        MediaTitle = e.Media.Title,
        CoverUrl = e.PersonalCoverUrl ?? e.Media.OfficialCoverUrl,
        CategoryName = e.Media.Category.Name,
        CategoryColorToken = e.Media.Category.ColorToken,
        Rating = e.Rating,
        Status = e.Status,
        Privacy = e.Privacy,
        ConsumedAt = e.ConsumedAt,
        CreatedAt = e.CreatedAt
    };

    /// <summary>Full entry page. Parameterised by viewer to resolve authorship.</summary>
    public static Expression<Func<Entry, EntryDetailDto>> ToDetailDto(string? viewerId) =>
        e => new EntryDetailDto
        {
            Id = e.Id,
            MediaId = e.MediaId,
            MediaTitle = e.Media.Title,
            MediaOfficialCoverUrl = e.Media.OfficialCoverUrl,
            MediaReleaseYear = e.Media.ReleaseYear,
            MediaEntryCount = e.Media.EntryCount,
            CategoryName = e.Media.Category.Name,
            CategoryColorToken = e.Media.Category.ColorToken,
            PersonalCoverUrl = e.PersonalCoverUrl,
            Rating = e.Rating,
            Review = e.Review,
            FavoriteMoment = e.FavoriteMoment,
            Status = e.Status,
            Privacy = e.Privacy,
            ConsumedAt = e.ConsumedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            AuthorId = e.UserId,
            AuthorUserName = e.User.UserName!,
            AuthorDisplayName = e.User.DisplayName,
            AuthorProfilePictureUrl = e.User.ProfilePictureUrl,
            Tags = e.EntryTags
                    .Select(et => new TagDto { Id = et.TagId, Name = et.Tag.Name })
                    .ToList(),
            LikeCount = e.Likes.Count(),
            CommentCount = e.Comments.Count(),
            ViewerIsAuthor = viewerId != null && e.UserId == viewerId,
            ViewerHasLiked = viewerId != null && e.Likes.Any(l => l.UserId == viewerId)
        };

    public static Entry ToEntity(this CreateEntryRequest request, string userId, DateTime utcNow)
        => new()
        {
            UserId = userId,
            MediaId = request.MediaId,
            Rating = request.Rating,
            Review = Clean(request.Review),
            FavoriteMoment = Clean(request.FavoriteMoment),
            Status = request.Status,
            Privacy = request.Privacy,
            PersonalCoverUrl = Clean(request.PersonalCoverUrl),
            ConsumedAt = request.ConsumedAt,
            CreatedAt = utcNow
        };

    public static void ApplyTo(this UpdateEntryRequest request, Entry entry, DateTime utcNow)
    {
        entry.Rating = request.Rating;
        entry.Review = Clean(request.Review);
        entry.FavoriteMoment = Clean(request.FavoriteMoment);
        entry.Status = request.Status;
        entry.Privacy = request.Privacy;
        entry.PersonalCoverUrl = Clean(request.PersonalCoverUrl);
        entry.ConsumedAt = request.ConsumedAt;
        entry.UpdatedAt = utcNow;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
