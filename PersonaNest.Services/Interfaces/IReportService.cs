using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;

namespace PersonaNest.Services.Interfaces;

/// <summary>
/// Reporting content (Specification v3 §6). The moderator-facing side - reading and
/// resolving the queue - lives on <see cref="IAdminService"/>; this is the user-facing side,
/// available to any signed-in user.
/// </summary>
public interface IReportService
{
    Task<ServiceResult> SubmitAsync(
        CreateReportRequest request, string reporterId, CancellationToken cancellationToken = default);
}
