using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Profiles, appearance and the taste profile (§16, §22).</summary>
public interface IProfileService
{
    Task<ProfileHeaderDto?> GetByUserNameAsync(
        string userName, string? viewerId, CancellationToken cancellationToken = default);

    Task<ProfileStatsDto> GetStatsAsync(
        string userId, CancellationToken cancellationToken = default);

    /// <summary>Null until the background service has computed one (§26).</summary>
    Task<TasteProfileDto?> GetTasteProfileAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateProfileAsync(
        string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAppearanceAsync(
        string userId, UpdateAppearanceRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdatePrivacyAsync(
        string userId, UpdatePrivacyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ThemeDto>> GetThemesAsync(CancellationToken cancellationToken = default);
}
