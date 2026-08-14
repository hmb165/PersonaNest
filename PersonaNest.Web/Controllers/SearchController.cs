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
    private readonly IMediaService _mediaService;

    public SearchController(IMediaService mediaService)
    {
        _mediaService = mediaService;
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

        return View(model);
    }
}
