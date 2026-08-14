using System.ComponentModel.DataAnnotations;
using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Requests;

/// <summary>Settings -&gt; Public Profile.</summary>
public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(60, MinimumLength = 1)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Bio")]
    public string? Bio { get; set; }

    [StringLength(400)]
    [Display(Name = "Profile picture URL")]
    public string? ProfilePictureUrl { get; set; }

    [StringLength(400)]
    [Display(Name = "Banner URL")]
    public string? BannerUrl { get; set; }
}

/// <summary>Settings -&gt; Appearance. Theme selects an accent preset; AccentColor overrides it.</summary>
public sealed class UpdateAppearanceRequest
{
    [Display(Name = "Theme")]
    public int? ThemeId { get; set; }

    [RegularExpression("^#[0-9a-fA-F]{6}$",
        ErrorMessage = "Enter a colour as a 6-digit hex value, for example #7c5cfc.")]
    [StringLength(7)]
    [Display(Name = "Custom accent colour")]
    public string? AccentColor { get; set; }
}

/// <summary>Settings -&gt; Privacy. Controls the default applied to newly created entries.</summary>
public sealed class UpdatePrivacyRequest
{
    [Required]
    [Display(Name = "Default privacy for new entries")]
    public Privacy DefaultEntryPrivacy { get; set; } = Privacy.Public;
}
