using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>Comments on an Entry, with one level of replies (§5, §18). Signed-in users only.</summary>
[Authorize]
public class CommentsController : Controller
{
    private readonly ICommentService _commentService;
    private readonly IReportService _reportService;

    public CommentsController(ICommentService commentService, IReportService reportService)
    {
        _commentService = commentService;
        _reportService = reportService;
    }

    /// <summary>
    /// POST /Comments/Create. Redisplays the Entry Details page with the validation error kept
    /// in <c>TempData</c> rather than redisplaying the whole page server-side - the comment
    /// thread is a small part of a much larger page, and a fresh GET is simpler than threading
    /// ModelState through EntryDetailsViewModel.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCommentRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        if (ModelState.IsValid)
        {
            var result = await _commentService.CreateAsync(form, userId, cancellationToken);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.FirstError;
            }
        }
        else
        {
            TempData["Error"] = "Write something before posting.";
        }

        return RedirectToAction("Details", "Entries", new { id = form.EntryId });
    }

    /// <summary>POST /Comments/Delete/{id}</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int entryId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var result = await _commentService.DeleteAsync(id, userId, cancellationToken);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
        }

        return RedirectToAction("Details", "Entries", new { id = entryId });
    }

    /// <summary>POST /Comments/Like/{id}</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id, int entryId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var result = await _commentService.ToggleLikeAsync(userId, id, cancellationToken);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
        }

        return RedirectToAction("Details", "Entries", new { id = entryId });
    }

    /// <summary>POST /Comments/Report/{id}</summary>
    [HttpPost("Comments/Report/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(
        int id, int entryId, ReportReason reason, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var result = await _reportService.SubmitAsync(
            new CreateReportRequest { TargetType = ReportTargetType.Comment, TargetId = id, Reason = reason },
            userId, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Thanks - a moderator will take a look." : result.FirstError;

        return RedirectToAction("Details", "Entries", new { id = entryId });
    }
}
