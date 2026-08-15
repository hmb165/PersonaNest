using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Domain.Constants;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// Applying to moderate (§7) and the moderator overview (§23, §6).
/// <para>
/// Deliberately no class-level <c>[Authorize(Roles = ...)]</c> - <see cref="Apply"/> is for any
/// signed-in user (that's the whole point: users without the role apply for it), while the
/// dashboard/queue/media actions are Moderator/Admin only. A class-level role restriction would
/// apply to every action including Apply, the same trap documented on <c>MediaController</c>.
/// </para>
/// </summary>
[Authorize]
public class ModerationController : Controller
{
    private const string ModeratorOrAdmin = $"{Roles.Moderator},{Roles.Admin}";

    private readonly IAdminService _adminService;
    private readonly IModeratorApplicationService _applicationService;
    private readonly IMediaService _mediaService;

    public ModerationController(
        IAdminService adminService,
        IModeratorApplicationService applicationService,
        IMediaService mediaService)
    {
        _adminService = adminService;
        _applicationService = applicationService;
        _mediaService = mediaService;
    }

    /// <summary>GET /Moderation/Apply</summary>
    [HttpGet]
    public async Task<IActionResult> Apply(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var model = new ModeratorApplicationViewModel
        {
            ExistingApplication = await _applicationService.GetLatestForUserAsync(userId, cancellationToken)
        };

        return View(model);
    }

    /// <summary>POST /Moderation/Apply</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        SubmitModeratorApplicationRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        if (!ModelState.IsValid)
        {
            return await RedisplayApplyAsync(form, cancellationToken);
        }

        var result = await _applicationService.SubmitAsync(userId, form, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.FirstError!);
            return await RedisplayApplyAsync(form, cancellationToken);
        }

        TempData["Success"] = "Your application was submitted. An admin will review it soon.";
        return RedirectToAction(nameof(Apply));
    }

    /// <summary>GET /Moderation/Dashboard</summary>
    [HttpGet]
    [Authorize(Roles = ModeratorOrAdmin)]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var stats = await _adminService.GetDashboardStatsAsync(cancellationToken);
        var resolvedSince = DateTime.UtcNow.AddDays(-30);

        var recent = await _adminService.GetReportQueueAsync(
            status: ReportStatus.Open, targetType: null, page: 1, pageSize: 10, cancellationToken);

        var model = new ModeratorDashboardViewModel
        {
            OpenReports = stats.OpenReports,
            MediaAwaitingReview = stats.MediaAwaitingReview,
            ResolvedLast30Days = await _adminService.CountResolvedReportsSinceAsync(
                resolvedSince, cancellationToken),
            RecentReports = recent.Items
        };

        return View(model);
    }

    /// <summary>GET /Moderation/Reports</summary>
    [HttpGet]
    [Authorize(Roles = ModeratorOrAdmin)]
    public async Task<IActionResult> Reports(
        ReportStatus? status, ReportTargetType? targetType, int page, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;

        // Default to the Open queue - an unfiltered "everything ever reported" view is rarely
        // what a moderator wants to land on.
        var effectiveStatus = status ?? ReportStatus.Open;

        var results = await _adminService.GetReportQueueAsync(
            effectiveStatus, targetType, page, 20, cancellationToken);

        var model = new ReportQueueViewModel
        {
            Status = effectiveStatus,
            TargetType = targetType,
            Items = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        return View(model);
    }

    /// <summary>POST /Moderation/Reports/{targetType}/Resolve/{id}</summary>
    [HttpPost("Moderation/Reports/{targetType}/Resolve/{id:int}")]
    [Authorize(Roles = ModeratorOrAdmin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveReport(
        ReportTargetType targetType, int id, string? notes, CancellationToken cancellationToken)
    {
        var moderatorId = User.GetUserId()!;
        var result = await _adminService.ResolveReportAsync(
            targetType, id, moderatorId, notes, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Report resolved." : result.FirstError;

        return RedirectToAction(nameof(Reports));
    }

    /// <summary>POST /Moderation/Reports/{targetType}/Dismiss/{id}</summary>
    [HttpPost("Moderation/Reports/{targetType}/Dismiss/{id:int}")]
    [Authorize(Roles = ModeratorOrAdmin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissReport(
        ReportTargetType targetType, int id, string? notes, CancellationToken cancellationToken)
    {
        var moderatorId = User.GetUserId()!;
        var result = await _adminService.DismissReportAsync(
            targetType, id, moderatorId, notes, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Report dismissed." : result.FirstError;

        return RedirectToAction(nameof(Reports));
    }

    /// <summary>GET /Moderation/Media - media items with at least one open report.</summary>
    [HttpGet]
    [Authorize(Roles = ModeratorOrAdmin)]
    public async Task<IActionResult> Media(int page, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;

        var results = await _adminService.GetReportQueueAsync(
            status: ReportStatus.Open, targetType: ReportTargetType.Media,
            page, 20, cancellationToken);

        var model = new ReportQueueViewModel
        {
            Status = ReportStatus.Open,
            TargetType = ReportTargetType.Media,
            Items = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        return View(model);
    }

    private async Task<IActionResult> RedisplayApplyAsync(
        SubmitModeratorApplicationRequest form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var model = new ModeratorApplicationViewModel
        {
            Form = form,
            ExistingApplication = await _applicationService.GetLatestForUserAsync(userId, cancellationToken)
        };

        return View(nameof(Apply), model);
    }
}
