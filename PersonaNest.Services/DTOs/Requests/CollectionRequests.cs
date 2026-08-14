using System.ComponentModel.DataAnnotations;
using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Requests;

/// <summary>Creating a collection (§20).</summary>
public sealed class CreateCollectionRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Privacy")]
    public Privacy Privacy { get; set; } = Privacy.Public;
}

/// <summary>Editing a collection.</summary>
public sealed class UpdateCollectionRequest
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public Privacy Privacy { get; set; }
}

/// <summary>Adding a media item to a collection.</summary>
public sealed class AddCollectionItemRequest
{
    [Required]
    public int CollectionId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int MediaId { get; set; }
}
