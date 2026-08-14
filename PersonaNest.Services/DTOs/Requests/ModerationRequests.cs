using System.ComponentModel.DataAnnotations;

namespace PersonaNest.Services.DTOs.Requests;

/// <summary>The Apply to Moderate form (§7). Two fields, matching the design.</summary>
public sealed class SubmitModeratorApplicationRequest
{
    [Required(ErrorMessage = "Please tell us why you want to moderate.")]
    [StringLength(2000, MinimumLength = 30,
        ErrorMessage = "Please write at least 30 characters.")]
    [Display(Name = "Why do you want to moderate PersonaNest?")]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Relevant experience (optional)")]
    public string? RelevantExperience { get; set; }
}

/// <summary>An admin approving or rejecting an application.</summary>
public sealed class ReviewModeratorApplicationRequest
{
    [Required]
    public int ApplicationId { get; set; }

    /// <summary>True to approve and assign the Moderator role; false to reject.</summary>
    [Required]
    public bool Approve { get; set; }

    [StringLength(2000)]
    [Display(Name = "Notes")]
    public string? AdminNotes { get; set; }
}

/// <summary>Banning a user. Enforcement uses Identity lockout (decision D-9).</summary>
public sealed class BanUserRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "A reason is required.")]
    [StringLength(300, MinimumLength = 3)]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Null bans indefinitely; otherwise the lockout expires at this instant (UTC).</summary>
    public DateTime? BannedUntil { get; set; }
}

/// <summary>Admin -&gt; Category Management (§15).</summary>
public sealed class SaveCategoryRequest
{
    /// <summary>Null when creating.</summary>
    public int? Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(60, MinimumLength = 1)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(8)]
    [Display(Name = "Icon")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "Slug is required.")]
    [StringLength(64, MinimumLength = 1)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "Use lowercase letters, digits and hyphens, for example tv-shows.")]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    [Display(Name = "Colour token")]
    public string ColorToken { get; set; } = "primary";
}
