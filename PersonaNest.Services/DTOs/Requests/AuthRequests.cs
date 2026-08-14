using System.ComponentModel.DataAnnotations;

namespace PersonaNest.Services.DTOs.Requests;

/// <summary>
/// The Log in form (§14). Data annotations drive both the unobtrusive client-side validation and
/// the server-side <c>ModelState</c> check - client validation is never trusted on its own (§12).
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Either the account's email address or its username. The login flow decides which lookup
    /// to use - an <c>@</c> means email, since usernames are restricted to letters, digits and
    /// underscores (see <see cref="RegisterRequest.UserName"/>).
    /// </summary>
    [Required(ErrorMessage = "Enter your email or username.")]
    [Display(Name = "Email or Username")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }
}

/// <summary>
/// The Join form (§14). Password rules mirror the Identity options configured in
/// <c>AddInfrastructure</c>: at least 8 characters, with an upper case letter, a lower case
/// letter and a digit.
/// </summary>
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Choose a username.")]
    [StringLength(30, MinimumLength = 3,
        ErrorMessage = "Username must be between 3 and 30 characters.")]
    [RegularExpression("^[a-zA-Z0-9_]+$",
        ErrorMessage = "Use letters, digits and underscores only.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a display name.")]
    [StringLength(60, MinimumLength = 1)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
