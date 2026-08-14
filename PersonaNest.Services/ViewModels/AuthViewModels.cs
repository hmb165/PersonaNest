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
