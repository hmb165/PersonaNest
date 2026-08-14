using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>
/// Manual Mapping for the reference-data entities.
/// <para>
/// Every member here is an <see cref="Expression{TDelegate}"/> so it can be handed to a
/// repository and translated into the SQL SELECT list. AutoMapper is not used anywhere in
/// PersonaNest.
/// </para>
/// </summary>
public static class LookupMappings
{
    public static Expression<Func<Category, CategoryDto>> ToCategoryDto => c => new CategoryDto
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        Icon = c.Icon,
        Slug = c.Slug,
        ColorToken = c.ColorToken,
        MediaCount = c.Media.Count()
    };

    public static Expression<Func<Tag, TagDto>> ToTagDto => t => new TagDto
    {
        Id = t.Id,
        Name = t.Name
    };

    public static Expression<Func<Theme, ThemeDto>> ToThemeDto => t => new ThemeDto
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        PrimaryHex = t.PrimaryHex,
        PrimaryDimHex = t.PrimaryDimHex,
        IsDefault = t.IsDefault
    };
}
