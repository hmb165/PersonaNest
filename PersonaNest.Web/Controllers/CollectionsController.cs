using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// Curated lists of media (§20). <see cref="Index"/> and <see cref="Details"/> are public where
/// the collection's own <c>Privacy</c> allows it (checked by <see cref="ICollectionService"/>);
/// every other action requires ownership, so only they carry <c>[Authorize]</c>.
/// </summary>
public class CollectionsController : Controller
{
    private readonly ICollectionService _collectionService;

    public CollectionsController(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    /// <summary>GET /Collections — the signed-in user's own collections.</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId()!;
        var results = await _collectionService.GetForUserAsync(userId, userId, page, 20, cancellationToken);

        var model = new CollectionsViewModel
        {
            OwnerUserName = User.Identity!.Name!,
            ViewerIsOwner = true,
            Collections = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page
        };

        return View(model);
    }

    /// <summary>POST /Collections/Create</summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "NewCollection")] CreateCollectionRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        if (!ModelState.IsValid)
        {
            return await RedisplayIndexAsync(form, cancellationToken);
        }

        var result = await _collectionService.CreateAsync(form, userId, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayIndexAsync(form, cancellationToken);
        }

        TempData["Success"] = $"\"{form.Name}\" was created.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    /// <summary>GET /Collections/Details/{id}</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var viewerId = User.GetUserId();
        var collection = await _collectionService.GetDetailsAsync(id, viewerId, cancellationToken);
        if (collection is null)
        {
            return NotFound();
        }

        var model = new CollectionDetailsViewModel
        {
            Collection = collection,
            EditForm = new UpdateCollectionRequest
            {
                Id = collection.Id,
                Name = collection.Name,
                Description = collection.Description,
                Privacy = collection.Privacy
            }
        };

        return View(model);
    }

    /// <summary>POST /Collections/Update/{id}</summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id, [Bind(Prefix = "EditForm")] UpdateCollectionRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        if (id != form.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            var result = await _collectionService.UpdateAsync(form, userId, cancellationToken);
            if (result.Succeeded)
            {
                TempData["Success"] = "Collection updated.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Error"] = result.FirstError;
        }
        else
        {
            TempData["Error"] = "Check the collection name and try again.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>POST /Collections/Delete/{id}</summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var result = await _collectionService.DeleteAsync(id, userId, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Collection deleted." : result.FirstError;

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST /Collections/AddItem. Posted from wherever the "+ Add to Collection" picker is shown
    /// (Media Details, Entry Details), so it redirects back there rather than to a fixed page.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(
        AddCollectionItemRequest form, string? returnUrl, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        if (ModelState.IsValid)
        {
            var result = await _collectionService.AddItemAsync(form, userId, cancellationToken);
            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Added to collection." : result.FirstError;
        }
        else
        {
            TempData["Error"] = "Choose a collection first.";
        }

        return LocalRedirect(
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(nameof(Index))!);
    }

    /// <summary>POST /Collections/RemoveItem/{id}</summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(
        int id, int mediaId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var result = await _collectionService.RemoveItemAsync(id, mediaId, userId, cancellationToken);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> RedisplayIndexAsync(
        CreateCollectionRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var results = await _collectionService.GetForUserAsync(userId, userId, 1, 20, cancellationToken);

        var model = new CollectionsViewModel
        {
            OwnerUserName = User.Identity!.Name!,
            ViewerIsOwner = true,
            Collections = results.Items,
            TotalCount = results.TotalCount,
            NewCollection = form
        };

        return View(nameof(Index), model);
    }
}
