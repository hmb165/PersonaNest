using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// Following users (§18). <see cref="Following"/> and <see cref="Followers"/> are public, like
/// the profile pages they're reached from; only <see cref="Follow"/> requires an account.
/// </summary>
public class SocialController : Controller
{
    private readonly IFollowService _followService;
    private readonly IProfileService _profileService;

    public SocialController(IFollowService followService, IProfileService profileService)
    {
        _followService = followService;
        _profileService = profileService;
    }

    /// <summary>GET /Social/Following?userName=...</summary>
    [HttpGet]
    [AllowAnonymous]
    public Task<IActionResult> Following(string? userName, int page, CancellationToken cancellationToken)
        => BuildListAsync("Following", userName, page, cancellationToken);

    /// <summary>GET /Social/Followers?userName=...</summary>
    [HttpGet]
    [AllowAnonymous]
    public Task<IActionResult> Followers(string? userName, int page, CancellationToken cancellationToken)
        => BuildListAsync("Followers", userName, page, cancellationToken);

    /// <summary>
    /// POST /Social/Follow/{id}. Toggles rather than a separate Unfollow route, matching the
    /// design's single listed route; <paramref name="id"/> is the target user's id.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(string id, string? returnUrl, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var alreadyFollowing = await _followService.IsFollowingAsync(userId, id, cancellationToken);
        var result = alreadyFollowing
            ? await _followService.UnfollowAsync(userId, id, cancellationToken)
            : await _followService.FollowAsync(userId, id, cancellationToken);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
        }

        return LocalRedirect(
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action("Index", "Home")!);
    }

    private async Task<IActionResult> BuildListAsync(
        string mode, string? userName, int page, CancellationToken cancellationToken)
    {
        var viewerId = User.GetUserId();
        var targetUserName = userName ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(targetUserName))
        {
            return Challenge();
        }

        var header = await _profileService.GetByUserNameAsync(targetUserName, viewerId, cancellationToken);
        if (header is null)
        {
            return NotFound();
        }

        page = page < 1 ? 1 : page;

        var results = mode == "Following"
            ? await _followService.GetFollowingAsync(header.Id, viewerId, page, 20, cancellationToken)
            : await _followService.GetFollowersAsync(header.Id, viewerId, page, 20, cancellationToken);

        var stats = await _profileService.GetStatsAsync(header.Id, cancellationToken);

        var model = new SocialListViewModel
        {
            ProfileUserName = header.UserName,
            ProfileDisplayName = header.DisplayName,
            Mode = mode,
            FollowingCount = stats.FollowingCount,
            FollowerCount = stats.FollowerCount,
            Users = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        return View("List", model);
    }
}
