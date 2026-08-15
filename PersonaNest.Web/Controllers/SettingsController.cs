using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// Privacy, notifications and account (§16). Signed-in users only. Profile and appearance moved
/// to <see cref="ProfileController.Edit"/> - Edit and Settings are two separate destinations, not
/// tabs on one page.
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
    public async Task<IActionResult> PrivacySettings(
        UpdatePrivacyRequest privacySettings, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(m => m.PrivacySettings = privacySettings, cancellationToken);
        }

        var result = await _profileService.UpdatePrivacyAsync(
            User.GetUserId()!, privacySettings, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayAsync(m => m.PrivacySettings = privacySettings, cancellationToken);
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
            UserName = header.UserName,
            MemberSince = header.CreatedAt,
            PrivacySettings = new UpdatePrivacyRequest
            {
                DefaultEntryPrivacy = header.DefaultEntryPrivacy
            },
            LatestApplication = await _moderatorApplicationService
                .GetLatestForUserAsync(userId, cancellationToken)
        };
    }
}
