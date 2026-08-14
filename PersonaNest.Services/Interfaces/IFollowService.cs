using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Following users (§18). One-way and instant - there is no approval step.</summary>
public interface IFollowService
{
    Task<ServiceResult> FollowAsync(
        string followerId, string targetUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult> UnfollowAsync(
        string followerId, string targetUserId, CancellationToken cancellationToken = default);

    Task<bool> IsFollowingAsync(
        string followerId, string targetUserId, CancellationToken cancellationToken = default);

    Task<PagedResult<UserCardDto>> GetFollowersAsync(
        string userId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<PagedResult<UserCardDto>> GetFollowingAsync(
        string userId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
