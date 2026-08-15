using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>The personal landing page after login (§23). Signed-in users only.</summary>
[Authorize]
public class DashboardController : Controller
{
    private readonly IEntryService _entryService;
    private readonly IProfileService _profileService;

    public DashboardController(IEntryService entryService, IProfileService profileService)
    {
        _entryService = entryService;
        _profileService = profileService;
    }

    /// <summary>GET /Dashboard</summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var recentEntries = await _entryService.GetForProfileAsync(
            userId, userId, page: 1, pageSize: 6, cancellationToken);

        var header = await _profileService.GetByUserNameAsync(
            User.Identity!.Name!, userId, cancellationToken);

        var model = new DashboardViewModel
        {
            DisplayName = header?.DisplayName ?? User.Identity!.Name!,
            Stats = await _profileService.GetStatsAsync(userId, cancellationToken),
            RecentEntries = recentEntries.Items,
            TasteProfile = await _profileService.GetTasteProfileAsync(userId, cancellationToken),
            FollowingActivity = await _entryService.GetFollowingFeedAsync(
                userId, page: 1, pageSize: 8, cancellationToken),
            EntriesThisWeek = await _entryService.CountCreatedSinceAsync(
                userId, DateTime.UtcNow.AddDays(-7), cancellationToken)
        };

        return View(model);
    }
}
