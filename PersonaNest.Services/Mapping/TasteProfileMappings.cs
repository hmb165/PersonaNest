using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>
/// Manual Mapping for the pre-computed taste profile (§22). Read-only here - the Phase 12
/// background service owns writing these rows.
/// </summary>
public static class TasteProfileMappings
{
    public static Expression<Func<TasteProfile, TasteProfileDto>> ToDto => tp => new TasteProfileDto
    {
        AverageRating = tp.AverageRating,
        TotalEntries = tp.TotalEntries,
        TotalReviews = tp.TotalReviews,
        MostActiveMonth = tp.MostActiveMonth,
        ComputedAt = tp.ComputedAt,
        AiNarrative = tp.AiNarrative,
        Categories = tp.Categories
            .OrderByDescending(c => c.Percentage)
            .Select(c => new TasteCategorySliceDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.Category.Name,
                CategoryColorToken = c.Category.ColorToken,
                Icon = c.Category.Icon,
                EntryCount = c.EntryCount,
                Percentage = c.Percentage
            })
            .ToList(),
        TopTags = tp.Tags
            .OrderByDescending(t => t.UseCount)
            .Take(10)
            .Select(t => new TasteTagDto
            {
                TagId = t.TagId,
                Name = t.Tag.Name,
                UseCount = t.UseCount
            })
            .ToList()
    };
}
