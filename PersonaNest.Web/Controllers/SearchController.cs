using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// Unified media search (§4, §6) — the entry point of the search -&gt; select-or-add -&gt; log
/// workflow. Public: a visitor must be able to search before they have an account.
/// </summary>
[AllowAnonymous]
public class SearchController : Controller
{
    // Seeded Category ids (CategoryConfiguration.cs) - matches MediaController.SearchExternal's
    // per-category provider dispatch.
    private const int CategoryAnime = 4;
    private const int CategoryBooks = 6;

    private readonly IMediaService _mediaService;
    private readonly IGoogleBooksService _googleBooksService;
    private readonly IKitsuService _kitsuService;

    public SearchController(
        IMediaService mediaService, IGoogleBooksService googleBooksService, IKitsuService kitsuService)
    {
        _mediaService = mediaService;
        _googleBooksService = googleBooksService;
        _kitsuService = kitsuService;
    }

    /// <summary>GET /Search</summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        MediaSearchRequest request, CancellationToken cancellationToken)
    {
        var results = await _mediaService.SearchAsync(request, cancellationToken);

        var model = new SearchViewModel
        {
            Filter = request,
            Categories = await _mediaService.GetCategoriesAsync(cancellationToken),
            Results = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        // Live Google Books results alongside PersonaNest's own catalogue, so a book search isn't
        // limited to whatever's already been logged locally. Scoped to the Books filter - the
        // same "category picks the provider" rule Media/SearchExternal already uses - rather than
        // guessing from free-text query on every search.
        if (request.CategoryId == CategoryBooks && !string.IsNullOrWhiteSpace(request.Query))
        {
            model.ExternalBookResults = await _googleBooksService.SearchAsync(
                request.Query, cancellationToken);
        }

        // Same idea for Anime, via Kitsu - no key needed.
        if (request.CategoryId == CategoryAnime && !string.IsNullOrWhiteSpace(request.Query))
        {
            model.ExternalAnimeResults = await _kitsuService.SearchAsync(
                request.Query, "anime", cancellationToken);
        }

        return View(model);
    }
}
