using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>Favourite media (§19). Signed-in users only.</summary>
[Authorize]
public class FavoritesController : Controller
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    /// <summary>GET /Favorites</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId()!;
        var results = await _favoriteService.GetForUserAsync(userId, page, 24, cancellationToken);

        var model = new FavoritesViewModel
        {
            Favorites = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        return View(model);
    }

    /// <summary>
    /// POST /Favorites/Toggle. Redirects back to wherever the button was clicked from (Media
    /// Details, Search, Home) rather than a fixed page, since favouriting is a one-click action
    /// on many different pages.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(
        int mediaId, string? returnUrl, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var result = await _favoriteService.ToggleAsync(userId, mediaId, cancellationToken);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
        }

        return LocalRedirect(
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(nameof(Index))!);
    }
}
