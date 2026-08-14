using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Applying for, and reviewing, the Moderator role (§7).</summary>
public interface IModeratorApplicationService
{
    /// <summary>
    /// Fails when the user already has a Pending application - mirrored by the filtered unique
    /// index, so the rule holds even under a race.
    /// </summary>
    Task<ServiceResult<int>> SubmitAsync(
        string userId, SubmitModeratorApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<ModeratorApplicationDto?> GetLatestForUserAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<PagedResult<ModeratorApplicationDto>> GetPendingAsync(
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<ModeratorApplicationDto?> GetByIdAsync(
        int applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve or reject. Approval assigns the Moderator Identity role, so the workflow of §7
    /// actually has an effect.
    /// </summary>
    Task<ServiceResult> ReviewAsync(
        ReviewModeratorApplicationRequest request, string adminId,
        CancellationToken cancellationToken = default);
}
