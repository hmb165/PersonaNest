using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// The public profile page (§16). Anonymous visitors may view it; the services decide what is
/// visible to them via the §18 privacy rules.
/// </summary>
[AllowAnonymous]
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
}
