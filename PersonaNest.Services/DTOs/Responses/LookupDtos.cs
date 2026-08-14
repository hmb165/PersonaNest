namespace PersonaNest.Services.DTOs.Responses;

/// <summary>A media category, with the presentation data the design system needs.</summary>
public sealed record CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>Emoji for the home-page pills.</summary>
    public string? Icon { get; init; }

    /// <summary>URL segment for <c>/Search?category=movies</c>.</summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>Design token driving the category badge colour.</summary>
    public string ColorToken { get; init; } = string.Empty;

    public int MediaCount { get; init; }
}

/// <summary>An entry tag.</summary>
public sealed record TagDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>An accent preset. PersonaNest is dark-only; a theme selects the accent pair.</summary>
public sealed record ThemeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string PrimaryHex { get; init; } = string.Empty;
    public string PrimaryDimHex { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}
