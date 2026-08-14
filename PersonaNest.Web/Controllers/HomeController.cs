using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Models;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// The public landing page. Phase 5 renders the hero and the seeded category pills; the
/// Trending and Community Activity rails fill in from Phase 6 onwards, once media and entries
/// exist.
/// </summary>
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IMediaService _mediaService;
    private readonly IEntryService _entryService;

    public HomeController(IMediaService mediaService, IEntryService entryService)
    {
        _mediaService = mediaService;
        _entryService = entryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new HomeViewModel
        {
            Categories = await _mediaService.GetCategoriesAsync(cancellationToken),
            Trending = await _mediaService.GetTrendingAsync(10, cancellationToken)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
}
