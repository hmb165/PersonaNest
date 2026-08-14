using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// A user's experience of a media item (§5, §7). Signed-in users only, except
/// <see cref="Details"/>: an entry's own <c>Privacy</c> decides who may see it, and that check
/// runs in <see cref="IEntryService.GetDetailsAsync"/> - not here.
/// </summary>
[Authorize]
public class EntriesController : Controller
{
    private readonly IEntryService _entryService;
    private readonly IMediaService _mediaService;
    private readonly ICollectionService _collectionService;
    private readonly ICommentService _commentService;

    public EntriesController(
        IEntryService entryService, IMediaService mediaService,
        ICollectionService collectionService, ICommentService commentService)
    {
        _entryService = entryService;
        _mediaService = mediaService;
        _collectionService = collectionService;
        _commentService = commentService;
    }

    /// <summary>GET /Entries</summary>
    [HttpGet]
    public async Task<IActionResult> Index(MyEntriesRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var results = await _entryService.GetMineAsync(userId, request, cancellationToken);

        var model = new MyEntriesViewModel
        {
            Filter = request,
            Categories = await _mediaService.GetCategoriesAsync(cancellationToken),
            Entries = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        return View(model);
    }

    /// <summary>GET /Entries/Create?mediaId=42</summary>
    [HttpGet]
    public async Task<IActionResult> Create(int? mediaId, CancellationToken cancellationToken)
    {
        if (mediaId is null)
        {
            TempData["Error"] = "Search for the media you want to log first.";
            return RedirectToAction("Index", "Search");
        }

        var userId = User.GetUserId()!;

        var media = await _mediaService.GetDetailsAsync(mediaId.Value, userId, cancellationToken);
        if (media is null)
        {
            TempData["Error"] = "That media item no longer exists.";
            return RedirectToAction("Index", "Search");
        }

        // Unique (UserId, MediaId): redirect to Edit instead of hitting the constraint (§5, D-11).
        var existingEntryId = await _entryService.FindExistingEntryIdAsync(
            userId, mediaId.Value, cancellationToken);

        if (existingEntryId is not null)
        {
            TempData["Info"] = $"You've already logged \"{media.Title}\" - here's your entry.";
            return RedirectToAction(nameof(Edit), new { id = existingEntryId });
        }

        var model = new EntryFormViewModel
        {
            IsEdit = false,
            Create = new CreateEntryRequest { MediaId = mediaId.Value },
            Media = media.AsCardDto(),
            AvailableTags = await _entryService.GetTagsAsync(cancellationToken)
        };

        return View("Form", model);
    }

    /// <summary>
    /// POST /Entries/Create. The Form view's fields are named "Create.Rating" etc. because the
    /// view's model is <see cref="EntryFormViewModel"/>; <c>[Bind(Prefix = "Create")]</c> tells
    /// the binder to read that same "Create." prefix but construct only the bare
    /// <see cref="CreateEntryRequest"/> - binding the whole ViewModel instead would also validate
    /// the untouched <c>Edit</c> half, and does for the reverse case: <c>Create.MediaId</c>'s
    /// <c>[Range(1, int.MaxValue)]</c> fails on its default 0 and silently fails <c>ModelOnly</c>
    /// validation (which only surfaces model-level errors, not this property-level one).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Create")] CreateEntryRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        if (!ModelState.IsValid)
        {
            return await RedisplayCreateAsync(form, cancellationToken);
        }

        var result = await _entryService.CreateAsync(form, userId, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayCreateAsync(form, cancellationToken);
        }

        TempData["Success"] = "Your entry was logged.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    /// <summary>GET /Entries/Edit/{id}</summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var entry = await _entryService.GetDetailsAsync(id, userId, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        if (!entry.ViewerIsAuthor)
        {
            return Forbid();
        }

        var model = new EntryFormViewModel
        {
            IsEdit = true,
            EntryId = entry.Id,
            Edit = new UpdateEntryRequest
            {
                Id = entry.Id,
                Rating = entry.Rating,
                Review = entry.Review,
                FavoriteMoment = entry.FavoriteMoment,
                Status = entry.Status,
                Privacy = entry.Privacy,
                PersonalCoverUrl = entry.PersonalCoverUrl,
                ConsumedAt = entry.ConsumedAt,
                TagIds = entry.Tags.Select(t => t.Id).ToList()
            },
            Media = new MediaCardDto
            {
                Id = entry.MediaId,
                Title = entry.MediaTitle,
                OfficialCoverUrl = entry.MediaOfficialCoverUrl,
                ReleaseYear = entry.MediaReleaseYear,
                CategoryName = entry.CategoryName,
                CategoryColorToken = entry.CategoryColorToken,
                EntryCount = entry.MediaEntryCount
            },
            AvailableTags = await _entryService.GetTagsAsync(cancellationToken),
            SelectedTagIds = entry.Tags.Select(t => t.Id).ToList()
        };

        return View("Form", model);
    }

    /// <summary>POST /Entries/Edit/{id} - see the note on the Create overload above.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, [Bind(Prefix = "Edit")] UpdateEntryRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        if (id != form.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return await RedisplayEditAsync(form, cancellationToken);
        }

        var result = await _entryService.UpdateAsync(form, userId, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayEditAsync(form, cancellationToken);
        }

        TempData["Success"] = "Your entry was updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>POST /Entries/Delete/{id}</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var result = await _entryService.DeleteAsync(id, userId, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
        }
        else
        {
            TempData["Success"] = "Entry deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>POST /Entries/Like/{id}</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var result = await _entryService.ToggleLikeAsync(userId, id, cancellationToken);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>GET /Entries/Details/{id}</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var viewerId = User.GetUserId();

        var entry = await _entryService.GetDetailsAsync(id, viewerId, cancellationToken);
        if (entry is null)
        {
            // Covers both "does not exist" and "exists but not visible to this viewer" -
            // deliberately the same response so a private entry's existence isn't leaked.
            return NotFound();
        }

        var model = new EntryDetailsViewModel
        {
            Entry = entry,
            ViewerCanComment = viewerId is not null,
            Comments = await _commentService.GetForEntryAsync(id, viewerId, cancellationToken)
        };

        if (viewerId is not null)
        {
            var myCollections = await _collectionService.GetForUserAsync(
                viewerId, viewerId, page: 1, pageSize: 50, cancellationToken);
            model.ViewerCollections = myCollections.Items;
        }

        return View(model);
    }

    private async Task<IActionResult> RedisplayCreateAsync(
        CreateEntryRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var media = await _mediaService.GetDetailsAsync(form.MediaId, userId, cancellationToken);

        var model = new EntryFormViewModel
        {
            IsEdit = false,
            Create = form,
            Media = media?.AsCardDto(),
            AvailableTags = await _entryService.GetTagsAsync(cancellationToken),
            SelectedTagIds = form.TagIds
        };

        return View("Form", model);
    }

    private async Task<IActionResult> RedisplayEditAsync(
        UpdateEntryRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var entry = await _entryService.GetDetailsAsync(form.Id, userId, cancellationToken);

        var model = new EntryFormViewModel
        {
            IsEdit = true,
            EntryId = form.Id,
            Edit = form,
            Media = entry is null ? null : new MediaCardDto
            {
                Id = entry.MediaId,
                Title = entry.MediaTitle,
                OfficialCoverUrl = entry.MediaOfficialCoverUrl,
                ReleaseYear = entry.MediaReleaseYear,
                CategoryName = entry.CategoryName,
                CategoryColorToken = entry.CategoryColorToken,
                EntryCount = entry.MediaEntryCount
            },
            AvailableTags = await _entryService.GetTagsAsync(cancellationToken),
            SelectedTagIds = form.TagIds
        };

        return View("Form", model);
    }
}
