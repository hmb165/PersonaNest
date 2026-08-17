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
    /// Profile header. Background and text accent are resolved independently - a Theme controls
    /// only the background wash, a custom AccentColor controls only a few text highlights, and
    /// neither one overrides the other.
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
            BackgroundColor = u.Theme != null ? u.Theme.PrimaryHex : DesignDefaults.AccentColor,
            AccentColor = u.AccentColor ?? DesignDefaults.AccentColor,
            CreatedAt = u.CreatedAt,
            DefaultEntryPrivacy = u.DefaultEntryPrivacy,
            IsViewerSelf = viewerId != null && u.Id == viewerId,
            IsFollowedByViewer =
                viewerId != null && u.Followers.Any(f => f.FollowerId == viewerId)
        };

    /// <summary>Raw appearance state (edit form) - see <see cref="AppearanceDto"/>.</summary>
    public static Expression<Func<ApplicationUser, AppearanceDto>> ToAppearanceDto =>
        u => new AppearanceDto { ThemeId = u.ThemeId, AccentColor = u.AccentColor };

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
