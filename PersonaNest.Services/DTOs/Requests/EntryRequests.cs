using System.ComponentModel.DataAnnotations;
using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Requests;

/// <summary>
/// The Create Entry form (§5). Rating is optional and, when present, must be 0.5-10.0 in 0.5
/// steps - matching the check constraints on the table (decision D-7).
/// </summary>
public sealed class CreateEntryRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Select a media item first.")]
    public int MediaId { get; set; }

    [Range(0.5, 10.0, ErrorMessage = "Rating must be between 0.5 and 10.0.")]
    [Display(Name = "Your Rating")]
    public decimal? Rating { get; set; }

    [StringLength(4000)]
    [Display(Name = "Review")]
    public string? Review { get; set; }

    [StringLength(500)]
    [Display(Name = "Favorite Moment")]
    public string? FavoriteMoment { get; set; }

    [Required]
    [Display(Name = "Status")]
    public EntryStatus Status { get; set; } = EntryStatus.Completed;

    [Required]
    [Display(Name = "Privacy")]
    public Privacy Privacy { get; set; } = Privacy.Public;

    [StringLength(400)]
    [Display(Name = "Personal cover image")]
    public string? PersonalCoverUrl { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime? ConsumedAt { get; set; }

    /// <summary>
    /// Tag ids selected on the form (§21). Concrete <see cref="List{T}"/>, not
    /// <c>IReadOnlyList&lt;int&gt;</c> - the checkbox group posts repeated "TagIds" values, and
    /// ASP.NET Core's model binder cannot construct an interface-typed collection property from
    /// that shape; it silently leaves it at the empty default instead of failing loudly.
    /// </summary>
    public List<int> TagIds { get; set; } = new();
}

/// <summary>The Edit Entry form. Media cannot change - that would be a different entry.</summary>
public sealed class UpdateEntryRequest
{
    [Required]
    public int Id { get; set; }

    [Range(0.5, 10.0, ErrorMessage = "Rating must be between 0.5 and 10.0.")]
    public decimal? Rating { get; set; }

    [StringLength(4000)]
    public string? Review { get; set; }

    [StringLength(500)]
    public string? FavoriteMoment { get; set; }

    [Required]
    public EntryStatus Status { get; set; }

    [Required]
    public Privacy Privacy { get; set; }

    [StringLength(400)]
    public string? PersonalCoverUrl { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ConsumedAt { get; set; }

    public List<int> TagIds { get; set; } = new();
}

/// <summary>Filters on the My Entries page.</summary>
public sealed class MyEntriesRequest
{
    public int? CategoryId { get; set; }
    public EntryStatus? Status { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
