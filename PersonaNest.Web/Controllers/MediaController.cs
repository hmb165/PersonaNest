using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// The community media catalogue (§4, §6): the shared Details page every Entry will point at,
/// and the Add Media form used when a search comes up empty.
/// <para>
/// Deliberately no class-level <c>[AllowAnonymous]</c>/<c>[Authorize]</c> — the two actions have
/// different audiences (Details is public, Add requires an account), and
/// <c>[AllowAnonymous]</c> on a class overrides <c>[Authorize]</c> on its actions, so the two
/// attributes are placed per-action instead.
/// </para>
/// </summary>
public class MediaController : Controller
{
    private readonly IMediaService _mediaService;
    private readonly IEntryService _entryService;
    private readonly ICollectionService _collectionService;
    private readonly IReportService _reportService;
    private readonly ITmdbService _tmdbService;
    private readonly IRawgService _rawgService;
    private readonly IGoogleBooksService _googleBooksService;
    private readonly IJikanService _jikanService;
    private readonly IMusicBrainzService _musicBrainzService;

    public MediaController(
        IMediaService mediaService, IEntryService entryService,
        ICollectionService collectionService, IReportService reportService,
        ITmdbService tmdbService, IRawgService rawgService,
        IGoogleBooksService googleBooksService, IJikanService jikanService,
        IMusicBrainzService musicBrainzService)
    {
        _mediaService = mediaService;
        _entryService = entryService;
        _collectionService = collectionService;
        _reportService = reportService;
        _tmdbService = tmdbService;
        _rawgService = rawgService;
        _googleBooksService = googleBooksService;
        _jikanService = jikanService;
        _musicBrainzService = musicBrainzService;
    }

    /// <summary>GET /Media/Details/{id}</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(
        int id, int page = 1, CancellationToken cancellationToken = default)
    {
        var viewerId = User.GetUserId();

        var media = await _mediaService.GetDetailsAsync(id, viewerId, cancellationToken);
        if (media is null)
        {
            return NotFound();
        }

        var entries = await _entryService.GetForMediaAsync(
            id, viewerId, page, pageSize: 20, cancellationToken);

        var model = new MediaDetailsViewModel
        {
            Media = media,
            CommunityEntries = entries.Items,
            CommunityEntryTotal = entries.TotalCount,
            Page = page
        };

        if (viewerId is not null)
        {
            var myCollections = await _collectionService.GetForUserAsync(
                viewerId, viewerId, page: 1, pageSize: 50, cancellationToken);
            model.ViewerCollections = myCollections.Items;
        }

        return View(model);
    }

    /// <summary>GET /Media/Add</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Add(CancellationToken cancellationToken)
    {
        var model = new AddMediaViewModel
        {
            Categories = await _mediaService.GetCategoriesAsync(cancellationToken)
        };

        return View(model);
    }

    /// <summary>
    /// GET /Media/SearchExternal?query=&amp;categoryId= - AJAX helper for the Add Media form
    /// (bonus: Consume an External API), one provider per category. Not part of the
    /// Swagger-documented REST API (Controllers/Api/) - this exists purely to keep every provider
    /// key server-side, never shipped to the browser.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> SearchExternal(
        string? query, int categoryId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(Array.Empty<ExternalSearchResultDto>());
        }

        var trimmed = query.Trim();

        // Seeded Category ids (CategoryConfiguration.cs): 1 Games, 2 Movies, 3 TV Shows,
        // 4 Anime, 5 Manga, 6 Books, 7 Music.
        var results = categoryId switch
        {
            1 => await _rawgService.SearchAsync(trimmed, cancellationToken),
            2 => await _tmdbService.SearchAsync(trimmed, "movie", cancellationToken),
            3 => await _tmdbService.SearchAsync(trimmed, "tv", cancellationToken),
            4 => await _jikanService.SearchAsync(trimmed, "anime", cancellationToken),
            5 => await _jikanService.SearchAsync(trimmed, "manga", cancellationToken),
            6 => await _googleBooksService.SearchAsync(trimmed, cancellationToken),
            7 => await _musicBrainzService.SearchAsync(trimmed, cancellationToken),
            _ => Array.Empty<ExternalSearchResultDto>()
        };

        return Json(results);
    }

    /// <summary>POST /Media/Add</summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(CreateMediaRequest form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAddAsync(form, cancellationToken);
        }

        var result = await _mediaService.CreateAsync(form, User.GetUserId()!, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayAddAsync(form, cancellationToken);
        }

        TempData["Success"] = $"\"{form.Title}\" was added to the database.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    /// <summary>POST /Media/Report/{id}</summary>
    [HttpPost("Media/Report/{id:int}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(
        int id, ReportReason reason, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var result = await _reportService.SubmitAsync(
            new CreateReportRequest { TargetType = ReportTargetType.Media, TargetId = id, Reason = reason },
            userId, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Thanks - a moderator will take a look." : result.FirstError;

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> RedisplayAddAsync(
        CreateMediaRequest form, CancellationToken cancellationToken)
    {
        var duplicates = string.IsNullOrWhiteSpace(form.Title)
            ? Array.Empty<MediaCardDto>()
            : await _mediaService.FindPossibleDuplicatesAsync(
                form.Title, form.CategoryId, form.ReleaseYear, cancellationToken);

        var model = new AddMediaViewModel
        {
            Form = form,
            Categories = await _mediaService.GetCategoriesAsync(cancellationToken),
            PossibleDuplicates = duplicates
        };

        return View(nameof(Add), model);
    }
}
