using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.ViewComponents;

/// <summary>Renders the navbar notification bell for signed-in users (Phase 15). Server-rendered
/// so the unread count and recent list are correct on first paint, before SignalR connects.</summary>
public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;

    public NotificationBellViewComponent(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.GetUserId();
        if (userId is null)
        {
            return Content(string.Empty);
        }

        var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
        var recent = await _notificationService.GetForUserAsync(userId, page: 1, pageSize: 5);

        var model = new NotificationBellViewModel
        {
            UnreadCount = unreadCount,
            Recent = recent.Items
        };

        return View(model);
    }
}
