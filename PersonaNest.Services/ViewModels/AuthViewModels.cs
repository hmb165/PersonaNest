using PersonaNest.Services.DTOs.Requests;

namespace PersonaNest.Services.ViewModels;

/// <summary>/Auth/Login. Wraps the request DTO so the return URL travels with the form.</summary>
public sealed class LoginViewModel
{
    public LoginRequest Form { get; set; } = new();
    public string? ReturnUrl { get; set; }
}

/// <summary>/Auth/Register.</summary>
public sealed class RegisterViewModel
{
    public RegisterRequest Form { get; set; } = new();
    public string? ReturnUrl { get; set; }
}

/// <summary>/Auth/ForgotPassword.</summary>
public sealed class ForgotPasswordViewModel
{
    public ForgotPasswordRequest Form { get; set; } = new();

    /// <summary>Set after a successful POST, so the view can swap the form for a confirmation message.</summary>
    public bool EmailSent { get; set; }
}

/// <summary>/Auth/ResetPassword. Email and Token arrive as query parameters from the emailed link.</summary>
public sealed class ResetPasswordViewModel
{
    public ResetPasswordRequest Form { get; set; } = new();
}
