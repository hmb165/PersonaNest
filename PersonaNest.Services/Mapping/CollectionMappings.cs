using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>Manual Mapping for <see cref="Collection"/> (§20).</summary>
public static class CollectionMappings
{
    /// <summary>Tile projection. Preview covers come from the first four items by position.</summary>
    public static Expression<Func<Collection, CollectionCardDto>> ToCardDto =>
        c => new CollectionCardDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Privacy = c.Privacy,
            ItemCount = c.Items.Count(),
            PreviewCoverUrls = c.Items
                .OrderBy(i => i.Position)
                .Select(i => i.Media.OfficialCoverUrl)
                .Where(url => url != null)
                .Take(4)
                .Select(url => url!)
                .ToList(),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

    public static Expression<Func<Collection, CollectionDetailDto>> ToDetailDto(string? viewerId) =>
        c => new CollectionDetailDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Privacy = c.Privacy,
            OwnerId = c.UserId,
            OwnerUserName = c.User.UserName!,
            OwnerDisplayName = c.User.DisplayName,
            ViewerIsOwner = viewerId != null && c.UserId == viewerId,
            Items = c.Items
                .OrderBy(i => i.Position)
                .Select(i => new CollectionItemDto
                {
                    MediaId = i.MediaId,
                    Title = i.Media.Title,
                    OfficialCoverUrl = i.Media.OfficialCoverUrl,
                    CategoryName = i.Media.Category.Name,
                    CategoryColorToken = i.Media.Category.ColorToken,
                    ReleaseYear = i.Media.ReleaseYear,
                    AverageRating = i.Media.AverageRating,
                    Position = i.Position,
                    AddedAt = i.AddedAt
                })
                .ToList(),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

    public static Collection ToEntity(this CreateCollectionRequest request, string userId, DateTime utcNow)
        => new()
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null : request.Description.Trim(),
            Privacy = request.Privacy,
            CreatedAt = utcNow
        };

    public static void ApplyTo(this UpdateCollectionRequest request, Collection collection, DateTime utcNow)
    {
        collection.Name = request.Name.Trim();
        collection.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null : request.Description.Trim();
        collection.Privacy = request.Privacy;
        collection.UpdatedAt = utcNow;
    }
}
