using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>Manual Mapping for <see cref="ApplicationUser"/>.</summary>
public static class UserMappings
{
    /// <summary>
    /// Profile header. The accent pair resolves in this order: the user's custom hex, then their
    /// selected theme, then the design default.
    /// </summary>
    public static Expression<Func<ApplicationUser, ProfileHeaderDto>> ToProfileHeaderDto(
        string? viewerId) =>
        u => new ProfileHeaderDto
        {
            Id = u.Id,
            UserName = u.UserName!,
            DisplayName = u.DisplayName,
            Bio = u.Bio,
            ProfilePictureUrl = u.ProfilePictureUrl,
            BannerUrl = u.BannerUrl,
            AccentColor = u.AccentColor
                ?? (u.Theme != null ? u.Theme.PrimaryHex : DesignDefaults.AccentColor),
            AccentColorDark = u.Theme != null
                ? u.Theme.PrimaryDimHex
                : DesignDefaults.AccentColorDark,
            CreatedAt = u.CreatedAt,
            IsViewerSelf = viewerId != null && u.Id == viewerId,
            IsFollowedByViewer =
                viewerId != null && u.Followers.Any(f => f.FollowerId == viewerId)
        };

    /// <summary>User row for search results and follower lists.</summary>
    public static Expression<Func<ApplicationUser, UserCardDto>> ToCardDto(string? viewerId) =>
        u => new UserCardDto
        {
            Id = u.Id,
            UserName = u.UserName!,
            DisplayName = u.DisplayName,
            ProfilePictureUrl = u.ProfilePictureUrl,
            EntryCount = u.Entries.Count(),
            CreatedAt = u.CreatedAt,
            IsFollowedByViewer =
                viewerId != null && u.Followers.Any(f => f.FollowerId == viewerId)
        };

    /// <summary>
    /// Admin user-management row. Roles are filled in by the service afterwards - role
    /// membership lives in Identity's tables and is read through UserManager, not projected here.
    /// </summary>
    public static Expression<Func<ApplicationUser, UserCardDto>> ToAdminCardDto =>
        u => new UserCardDto
        {
            Id = u.Id,
            UserName = u.UserName!,
            DisplayName = u.DisplayName,
            ProfilePictureUrl = u.ProfilePictureUrl,
            EntryCount = u.Entries.Count(),
            CreatedAt = u.CreatedAt,
            Email = u.Email,
            BanReason = u.BanReason,
            IsBanned = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
        };

    public static void ApplyTo(this UpdateProfileRequest request, ApplicationUser user)
    {
        user.DisplayName = request.DisplayName.Trim();
        user.Bio = Clean(request.Bio);
        user.ProfilePictureUrl = Clean(request.ProfilePictureUrl);
        user.BannerUrl = Clean(request.BannerUrl);
    }

    public static void ApplyTo(this UpdateAppearanceRequest request, ApplicationUser user)
    {
        user.ThemeId = request.ThemeId;
        user.AccentColor = Clean(request.AccentColor);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
