using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaNest.Domain.Constants;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.ViewModels;
using PersonaNest.Web.Extensions;

namespace PersonaNest.Web.Controllers;

/// <summary>
/// User management, media management, category management (§15) and reviewing moderator
/// applications (§7) - "Moderator Applications + Moderator/Admin functionality" (Phase 11).
/// Admin only throughout, so this is the one moderation-family controller that keeps a
/// class-level <see cref="AuthorizeAttribute"/> - every action here really is Admin-only, unlike
/// <c>ModerationController</c>, where Apply has a different audience.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IModeratorApplicationService _moderatorApplicationService;
    private readonly IMediaService _mediaService;

    public AdminController(
        IAdminService adminService,
        IModeratorApplicationService moderatorApplicationService,
        IMediaService mediaService)
    {
        _adminService = adminService;
        _moderatorApplicationService = moderatorApplicationService;
        _mediaService = mediaService;
    }

    /// <summary>GET /Admin</summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var recentUsers = await _adminService.GetUsersAsync(
            query: null, page: 1, pageSize: 5, cancellationToken);

        var pendingApplications = await _moderatorApplicationService.GetPendingAsync(
            page: 1, pageSize: 5, cancellationToken);

        var model = new AdminDashboardViewModel
        {
            Stats = await _adminService.GetDashboardStatsAsync(cancellationToken),
            RecentUsers = recentUsers.Items,
            PendingApplications = pendingApplications.Items
        };

        return View(model);
    }

    // ── Users ────────────────────────────────────────────────────────────────────────

    /// <summary>GET /Admin/Users</summary>
    [HttpGet]
    public async Task<IActionResult> Users(string? query, int page, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        var results = await _adminService.GetUsersAsync(query, page, 20, cancellationToken);

        var model = new UserManagementViewModel
        {
            Query = query,
            Users = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        return View(model);
    }

    /// <summary>GET /Admin/Users/Details/{id}</summary>
    [HttpGet("Admin/Users/Details/{id}")]
    public async Task<IActionResult> UserDetails(string id, CancellationToken cancellationToken)
    {
        var user = await _adminService.GetUserByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : View(user);
    }

    /// <summary>POST /Admin/Users/Ban/{id}</summary>
    [HttpPost("Admin/Users/Ban/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ban(string id, string reason, CancellationToken cancellationToken)
    {
        var result = await _adminService.BanAsync(
            new BanUserRequest { UserId = id, Reason = reason }, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Account banned." : result.FirstError;

        return RedirectToAction(nameof(UserDetails), new { id });
    }

    /// <summary>
    /// POST /Admin/Users/Unban/{id}. Not in the design's route list, which only shows Ban; added
    /// so a ban can actually be reversed, since <c>IAdminService.UnbanAsync</c> already exists.
    /// </summary>
    [HttpPost("Admin/Users/Unban/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unban(string id, CancellationToken cancellationToken)
    {
        var result = await _adminService.UnbanAsync(id, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Account unbanned." : result.FirstError;

        return RedirectToAction(nameof(UserDetails), new { id });
    }

    /// <summary>POST /Admin/Users/Promote/{id} - User to Moderator only.</summary>
    [HttpPost("Admin/Users/Promote/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Promote(string id, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _adminService.PromoteAsync(id, Roles.Moderator, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Promoted to Moderator." : result.FirstError;

        return RedirectToLocalOrDefault(returnUrl, nameof(UserDetails), new { id });
    }

    /// <summary>
    /// POST /Admin/Users/Demote/{id} - Moderator back to User. Not in the design's route list
    /// (which shows the mockup's "Demote" button but no route for it); added so the mockup's own
    /// button has somewhere to go, mirroring Promote/<c>IAdminService.DemoteAsync</c>.
    /// </summary>
    [HttpPost("Admin/Users/Demote/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Demote(string id, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _adminService.DemoteAsync(id, Roles.Moderator, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Demoted to User." : result.FirstError;

        return RedirectToLocalOrDefault(returnUrl, nameof(UserDetails), new { id });
    }

    // ── Media ────────────────────────────────────────────────────────────────────────

    /// <summary>GET /Admin/Media</summary>
    [HttpGet]
    public async Task<IActionResult> Media(MediaSearchRequest request, CancellationToken cancellationToken)
    {
        var results = await _mediaService.SearchAsync(request, cancellationToken);

        var model = new SearchViewModel
        {
            Filter = request,
            Categories = await _mediaService.GetCategoriesAsync(cancellationToken),
            Results = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };

        return View(model);
    }

    /// <summary>POST /Admin/Media/Delete/{id}</summary>
    [HttpPost("Admin/Media/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MediaDelete(int id, CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId()!;
        var result = await _mediaService.SoftDeleteAsync(id, adminId, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Media removed." : result.FirstError;

        return RedirectToAction(nameof(Media));
    }

    // ── Categories (§15) ────────────────────────────────────────────────────────────

    /// <summary>GET /Admin/Categories</summary>
    [HttpGet]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        var model = new CategoryManagementViewModel
        {
            Categories = await _adminService.GetCategoriesAsync(cancellationToken)
        };

        return View(model);
    }

    /// <summary>GET /Admin/Categories/Create</summary>
    [HttpGet("Admin/Categories/Create")]
    public async Task<IActionResult> CategoriesCreate(CancellationToken cancellationToken)
    {
        var model = new CategoryManagementViewModel
        {
            Categories = await _adminService.GetCategoriesAsync(cancellationToken)
        };

        return View("Categories", model);
    }

    /// <summary>POST /Admin/Categories/Create</summary>
    [HttpPost("Admin/Categories/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoriesCreate(
        SaveCategoryRequest form, CancellationToken cancellationToken)
        => await SaveCategoryAsync(form, cancellationToken);

    /// <summary>GET /Admin/Categories/Edit/{id}</summary>
    [HttpGet("Admin/Categories/Edit/{id:int}")]
    public async Task<IActionResult> CategoriesEdit(int id, CancellationToken cancellationToken)
    {
        var categories = await _adminService.GetCategoriesAsync(cancellationToken);
        var category = categories.FirstOrDefault(c => c.Id == id);
        if (category is null)
        {
            return NotFound();
        }

        var model = new CategoryManagementViewModel
        {
            Categories = categories,
            Form = new SaveCategoryRequest
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Icon = category.Icon,
                Slug = category.Slug,
                ColorToken = category.ColorToken
            }
        };

        return View("Categories", model);
    }

    /// <summary>POST /Admin/Categories/Edit/{id}</summary>
    [HttpPost("Admin/Categories/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoriesEdit(
        int id, SaveCategoryRequest form, CancellationToken cancellationToken)
    {
        form.Id = id;
        return await SaveCategoryAsync(form, cancellationToken);
    }

    /// <summary>POST /Admin/Categories/Delete/{id}</summary>
    [HttpPost("Admin/Categories/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoriesDelete(int id, CancellationToken cancellationToken)
    {
        var result = await _adminService.DeleteCategoryAsync(id, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Category deleted." : result.FirstError;

        return RedirectToAction(nameof(Categories));
    }

    // ── Moderator applications (§7) ─────────────────────────────────────────────────

    /// <summary>GET /Admin/Applications</summary>
    [HttpGet]
    public async Task<IActionResult> Applications(int page, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        var results = await _moderatorApplicationService.GetPendingAsync(page, 20, cancellationToken);

        var model = new ModeratorApplicationsViewModel
        {
            Applications = results.Items,
            TotalCount = results.TotalCount,
            Page = results.Page
        };

        return View(model);
    }

    /// <summary>POST /Admin/Applications/Approve/{id}</summary>
    [HttpPost("Admin/Applications/Approve/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplicationsApprove(
        int id, string? adminNotes, CancellationToken cancellationToken)
        => await ReviewApplicationAsync(id, approve: true, adminNotes, cancellationToken);

    /// <summary>POST /Admin/Applications/Reject/{id}</summary>
    [HttpPost("Admin/Applications/Reject/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplicationsReject(
        int id, string? adminNotes, CancellationToken cancellationToken)
        => await ReviewApplicationAsync(id, approve: false, adminNotes, cancellationToken);

    private async Task<IActionResult> ReviewApplicationAsync(
        int applicationId, bool approve, string? adminNotes, CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId()!;

        var result = await _moderatorApplicationService.ReviewAsync(
            new ReviewModeratorApplicationRequest
            {
                ApplicationId = applicationId,
                Approve = approve,
                AdminNotes = adminNotes
            },
            adminId,
            cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? (approve ? "Application approved - Moderator role granted." : "Application rejected.")
            : result.FirstError;

        return RedirectToAction(nameof(Applications));
    }

    private async Task<IActionResult> SaveCategoryAsync(
        SaveCategoryRequest form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _adminService.SaveCategoryAsync(form, cancellationToken);
            if (result.Succeeded)
            {
                TempData["Success"] = form.Id is null ? "Category created." : "Category updated.";
                return RedirectToAction(nameof(Categories));
            }

            ModelState.AddModelError(string.Empty, result.FirstError!);
        }

        var model = new CategoryManagementViewModel
        {
            Categories = await _adminService.GetCategoriesAsync(cancellationToken),
            Form = form
        };

        return View("Categories", model);
    }

    private IActionResult RedirectToLocalOrDefault(
        string? returnUrl, string fallbackAction, object fallbackRouteValues)
        => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(fallbackAction, fallbackRouteValues);
}
