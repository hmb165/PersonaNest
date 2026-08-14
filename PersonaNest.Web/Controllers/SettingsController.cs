using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// Profile, appearance and privacy settings (§16). Signed-in users only.
/// <para>
/// Thin by design (§10, §29): every action validates, delegates to a service, and redirects.
/// </para>
/// </summary>
[Authorize]
public class SettingsController : Controller
{
    private readonly IProfileService _profileService;
    private readonly IModeratorApplicationService _moderatorApplicationService;

    public SettingsController(
        IProfileService profileService,
        IModeratorApplicationService moderatorApplicationService)
    {
        _profileService = profileService;
        _moderatorApplicationService = moderatorApplicationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(cancellationToken);
        return model is null ? Challenge() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
        UpdateProfileRequest form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(m => m.Profile = form, cancellationToken);
        }

        var result = await _profileService.UpdateProfileAsync(
            User.GetUserId()!, form, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayAsync(m => m.Profile = form, cancellationToken);
        }

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Appearance(
        UpdateAppearanceRequest form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(m => m.Appearance = form, cancellationToken);
        }

        var result = await _profileService.UpdateAppearanceAsync(
            User.GetUserId()!, form, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayAsync(m => m.Appearance = form, cancellationToken);
        }

        TempData["Success"] = "Your appearance settings have been saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrivacySettings(
        UpdatePrivacyRequest form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(m => m.PrivacySettings = form, cancellationToken);
        }

        var result = await _profileService.UpdatePrivacyAsync(
            User.GetUserId()!, form, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayAsync(m => m.PrivacySettings = form, cancellationToken);
        }

        TempData["Success"] = "Your privacy setting has been saved.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RedisplayAsync(
        Action<SettingsViewModel> keepSubmittedValues, CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(cancellationToken);
        if (model is null)
        {
            return Challenge();
        }

        keepSubmittedValues(model);
        return View(nameof(Index), model);
    }

    private async Task<SettingsViewModel?> BuildViewModelAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return null;
        }

        var header = await _profileService.GetByUserNameAsync(
            User.Identity!.Name!, userId, cancellationToken);

        if (header is null)
        {
            return null;
        }

        return new SettingsViewModel
        {
            Profile = new UpdateProfileRequest
            {
                DisplayName = header.DisplayName,
                Bio = header.Bio,
                ProfilePictureUrl = header.ProfilePictureUrl,
                BannerUrl = header.BannerUrl
            },
            Appearance = new UpdateAppearanceRequest
            {
                AccentColor = header.AccentColor
            },
            Themes = await _profileService.GetThemesAsync(cancellationToken),
            LatestApplication = await _moderatorApplicationService
                .GetLatestForUserAsync(userId, cancellationToken)
        };
    }
}
