using System.ComponentModel.DataAnnotations;

namespace PersonaNest.Services.DTOs.Requests;

/// <summary>
/// The Add Media form (§4). Data annotations give the client-side unobtrusive validation of §12;
/// the service re-checks every rule server-side, because client validation is never trusted.
/// </summary>
public sealed class CreateMediaRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    /// <summary>Director, author, studio or artist - the form's "Creator / Studio / Author".</summary>
    [StringLength(200)]
    [Display(Name = "Creator / Studio / Author")]
    public string? Creator { get; set; }

    [Range(1800, 2200, ErrorMessage = "Enter a year between 1800 and 2200.")]
    [Display(Name = "Release Year")]
    public int? ReleaseYear { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Runtime (minutes)")]
    public int? RuntimeMinutes { get; set; }

    [StringLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(400)]
    [Url(ErrorMessage = "Enter a valid URL.")]
    [Display(Name = "Cover image URL")]
    public string? OfficialCoverUrl { get; set; }

    /// <summary>
    /// Set true once the user has seen the possible-duplicate list and chosen to continue.
    /// Keeps §4's duplicate guard advisory rather than absolute.
    /// </summary>
    public bool ConfirmedNotDuplicate { get; set; }
}

/// <summary>Editing an existing media row. Moderator or admin only.</summary>
public sealed class UpdateMediaRequest
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [StringLength(200)]
    public string? Creator { get; set; }

    [Range(1800, 2200)]
    public int? ReleaseYear { get; set; }

    [Range(1, 100000)]
    public int? RuntimeMinutes { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(400)]
    [Url]
    public string? OfficialCoverUrl { get; set; }
}

/// <summary>Query string of <c>/Search</c>.</summary>
public sealed class MediaSearchRequest
{
    [StringLength(200)]
    public string? Query { get; set; }

    public int? CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
