using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// The public profile page (§16), plus editing it. Viewing is anonymous; editing is the signed-in
/// owner only - <see cref="Edit"/>/<see cref="EditProfile"/>/<see cref="EditAppearance"/> carry
/// their own <see cref="AuthorizeAttribute"/> since <see cref="AllowAnonymousAttribute"/> on
/// <see cref="Index"/> would otherwise make the whole controller anonymous if it were applied at
/// the class level.
/// </summary>
public class ProfileController : Controller
{
    private const int RecentEntryCount = 6;
    private const int FavoritePreviewCount = 8;
    private const int CollectionPreviewCount = 6;

    private readonly IProfileService _profileService;
    private readonly IEntryService _entryService;
    private readonly IFavoriteService _favoriteService;
    private readonly ICollectionService _collectionService;

    public ProfileController(
        IProfileService profileService,
        IEntryService entryService,
        IFavoriteService favoriteService,
        ICollectionService collectionService)
    {
        _profileService = profileService;
        _entryService = entryService;
        _favoriteService = favoriteService;
        _collectionService = collectionService;
    }

    /// <summary>GET /Profile/{userName}</summary>
    [HttpGet("Profile/{userName}")]
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string userName, string tab = "Entries", CancellationToken cancellationToken = default)
    {
        var viewerId = User.GetUserId();

        var header = await _profileService.GetByUserNameAsync(userName, viewerId, cancellationToken);
        if (header is null)
        {
            return NotFound();
        }

        var entries = await _entryService.GetForProfileAsync(
            header.Id, viewerId, 1, RecentEntryCount, cancellationToken);

        var favorites = await _favoriteService.GetForUserAsync(
            header.Id, 1, FavoritePreviewCount, cancellationToken);

        var collections = await _collectionService.GetForUserAsync(
            header.Id, viewerId, 1, CollectionPreviewCount, cancellationToken);

        var model = new ProfileViewModel
        {
            Header = header,
            Stats = await _profileService.GetStatsAsync(header.Id, cancellationToken),
            TasteProfile = await _profileService.GetTasteProfileAsync(header.Id, cancellationToken),
            RecentEntries = entries.Items,
            Favorites = favorites.Items,
            Collections = collections.Items,
            ActiveTab = tab
        };

        return View(model);
    }

    /// <summary>GET /Profile/Edit - profile + appearance, separate from /Settings (§16 rework).</summary>
    [HttpGet("Profile/Edit")]
    [Authorize]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var model = await BuildEditViewModelAsync(cancellationToken);
        return model is null ? Challenge() : View(model);
    }

    /// <summary>POST /Profile/Edit/Profile</summary>
    [HttpPost("Profile/Edit/Profile")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(
        UpdateProfileRequest profile, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayEditAsync(m => m.Profile = profile, cancellationToken);
        }

        var result = await _profileService.UpdateProfileAsync(
            User.GetUserId()!, profile, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayEditAsync(m => m.Profile = profile, cancellationToken);
        }

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Edit));
    }

    /// <summary>POST /Profile/Edit/Appearance</summary>
    [HttpPost("Profile/Edit/Appearance")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAppearance(
        UpdateAppearanceRequest appearance, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayEditAsync(m => m.Appearance = appearance, cancellationToken);
        }

        var result = await _profileService.UpdateAppearanceAsync(
            User.GetUserId()!, appearance, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayEditAsync(m => m.Appearance = appearance, cancellationToken);
        }

        TempData["Success"] = "Your appearance settings have been saved.";
        return RedirectToAction(nameof(Edit));
    }

    private async Task<IActionResult> RedisplayEditAsync(
        Action<EditProfileViewModel> keepSubmittedValues, CancellationToken cancellationToken)
    {
        var model = await BuildEditViewModelAsync(cancellationToken);
        if (model is null)
        {
            return Challenge();
        }

        keepSubmittedValues(model);
        return View(nameof(Edit), model);
    }

    private async Task<EditProfileViewModel?> BuildEditViewModelAsync(CancellationToken cancellationToken)
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

        var appearance = await _profileService.GetAppearanceAsync(userId, cancellationToken);

        return new EditProfileViewModel
        {
            UserName = header.UserName,
            Profile = new UpdateProfileRequest
            {
                DisplayName = header.DisplayName,
                Bio = header.Bio,
                ProfilePictureUrl = header.ProfilePictureUrl,
                BannerUrl = header.BannerUrl
            },
            // Raw state, not header.AccentColor - that's already resolved through the theme
            // fallback, which would make every profile look like it has a custom colour set.
            Appearance = new UpdateAppearanceRequest
            {
                ThemeId = appearance?.ThemeId,
                AccentColor = appearance?.AccentColor
            },
            Themes = await _profileService.GetThemesAsync(cancellationToken)
        };
    }
}
