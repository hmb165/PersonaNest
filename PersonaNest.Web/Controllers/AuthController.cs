using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Domain.Constants;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// Register, Login, Logout and Access Denied (§14).
/// <para>
/// This is the one controller permitted to take <see cref="UserManager{TUser}"/> and
/// <see cref="SignInManager{TUser}"/> directly (approved decision D-13). Authentication is the
/// framework's own boundary; wrapping it would mean reimplementing password hashing, lockout and
/// security stamps behind a pass-through interface. Every other user operation goes through the
/// service layer.
/// </para>
/// </summary>
[AllowAnonymous]
public class AuthController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IProfileService _profileService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IProfileService profileService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _profileService = profileService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        // Server-side validation runs regardless of what the browser did (§12).
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Accepts either identifier (§14 / design system: "Email or Username"). An '@' means
        // email, since RegisterRequest.UserName only ever allows letters, digits and
        // underscores - the two spaces can never collide.
        var identifier = model.Form.Identifier.Trim();
        var user = identifier.Contains('@')
            ? await _userManager.FindByEmailAsync(identifier)
            : await _userManager.FindByNameAsync(identifier);

        if (user is null || user.IsDeleted)
        {
            // Deliberately vague: revealing which half was wrong helps account enumeration.
            ModelState.AddModelError(string.Empty, "Incorrect email/username or password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Form.Password, model.Form.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserName} signed in.", user.UserName);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            // Covers both a temporary lockout and an admin ban, which is the same mechanism
            // (decision D-9).
            ModelState.AddModelError(string.Empty,
                "This account is locked. If you believe this is a mistake, contact a moderator.");
            _logger.LogWarning("Locked-out sign-in attempt for {UserName}.", user.UserName);
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Incorrect email/username or password.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _userManager.FindByNameAsync(model.Form.UserName) is not null)
        {
            ModelState.AddModelError("Form.UserName", "That username is already taken.");
            return View(model);
        }

        if (await _userManager.FindByEmailAsync(model.Form.Email) is not null)
        {
            ModelState.AddModelError("Form.Email", "That email address is already registered.");
            return View(model);
        }

        // New accounts start on the default theme so their profile renders with the design
        // system's accent rather than a null colour.
        var themes = await _profileService.GetThemesAsync(cancellationToken);
        var defaultTheme = themes.FirstOrDefault(t => t.IsDefault) ?? themes.FirstOrDefault();

        var user = new ApplicationUser
        {
            UserName = model.Form.UserName,
            Email = model.Form.Email,
            DisplayName = model.Form.DisplayName.Trim(),
            EmailConfirmed = true,
            ThemeId = defaultTheme?.Id,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userManager.CreateAsync(user, model.Form.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Every account starts in the User role (§6).
        await _userManager.AddToRoleAsync(user, Roles.User);

        _logger.LogInformation("New account registered: {UserName}.", user.UserName);

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(model.ReturnUrl);
    }

    /// <summary>
    /// POST only, with an anti-forgery token. The design's route map shows
    /// <c>GET /Auth/Logout</c>; a GET sign-out can be triggered by any third-party page, so this
    /// deviates deliberately. Reported in the Phase 5 report.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {UserName} signed out.", userName);

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    /// <summary>Blocks open-redirect attempts through the returnUrl parameter.</summary>
    private IActionResult RedirectToLocal(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(HomeController.Index), "Home");
}
