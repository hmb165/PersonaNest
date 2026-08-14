using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public MediaController(
        IMediaService mediaService, IEntryService entryService, ICollectionService collectionService)
    {
        _mediaService = mediaService;
        _entryService = entryService;
        _collectionService = collectionService;
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
