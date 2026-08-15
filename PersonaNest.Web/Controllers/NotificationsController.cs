using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>Notification history and read-state actions (Phase 15). Live delivery is SignalR's
/// job (<see cref="Hubs.NotificationHub"/>); everything here works with JavaScript off too.</summary>
[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId()!;
        page = page < 1 ? 1 : page;

        var results = await _notificationService.GetForUserAsync(userId, page, 20, cancellationToken);

        var model = new NotificationsViewModel
        {
            Notifications = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);

        return LocalRedirect(
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(nameof(Index))!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(string? returnUrl, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);

        return LocalRedirect(
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(nameof(Index))!);
    }
}
