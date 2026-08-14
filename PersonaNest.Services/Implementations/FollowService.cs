using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IFollowService"/>
public class FollowService : IFollowService
{
    private readonly IUnitOfWork _uow;

    public FollowService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<ServiceResult> FollowAsync(
        string followerId, string targetUserId, CancellationToken cancellationToken = default)
    {
        // Mirrors CK_Follow_NotSelf, so the user gets a message rather than a database error.
        if (string.Equals(followerId, targetUserId, StringComparison.Ordinal))
        {
            return ServiceResult.Failure("You cannot follow yourself.");
        }

        var targetExists = await _uow.Repository<ApplicationUser>()
            .AnyAsync(u => u.Id == targetUserId && !u.IsDeleted, cancellationToken);

        if (!targetExists)
        {
            return ServiceResult.Failure("That account no longer exists.");
        }

        var repository = _uow.Repository<Follow>();

        // Mirrors the unique (FollowerId, FollowingId) index.
        var already = await repository.AnyAsync(
            f => f.FollowerId == followerId && f.FollowingId == targetUserId, cancellationToken);

        if (already)
        {
            return ServiceResult.Success();
        }

        await repository.AddAsync(new Follow
        {
            FollowerId = followerId,
            FollowingId = targetUserId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UnfollowAsync(
        string followerId, string targetUserId, CancellationToken cancellationToken = default)
    {
        var repository = _uow.Repository<Follow>();

        var id = await repository.FirstOrDefaultAsync(
            f => f.FollowerId == followerId && f.FollowingId == targetUserId,
            f => (int?)f.Id,
            cancellationToken);

        if (id is not { } followId)
        {
            return ServiceResult.Success();
        }

        var tracked = await repository.GetByIdAsync(followId, cancellationToken);
        if (tracked is not null)
        {
            repository.Remove(tracked);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }

    public Task<bool> IsFollowingAsync(
        string followerId, string targetUserId, CancellationToken cancellationToken = default)
        => _uow.Repository<Follow>().AnyAsync(
            f => f.FollowerId == followerId && f.FollowingId == targetUserId, cancellationToken);

    public async Task<PagedResult<UserCardDto>> GetFollowersAsync(
        string userId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var users = _uow.Repository<ApplicationUser>();

        var items = await users.ListAsync(
            u => u.Following.Any(f => f.FollowingId == userId),
            UserMappings.ToCardDto(viewerId),
            q => q.OrderBy(u => u.DisplayName),
            page, pageSize, cancellationToken);

        var total = await users.CountAsync(
            u => u.Following.Any(f => f.FollowingId == userId), cancellationToken);

        return new PagedResult<UserCardDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<UserCardDto>> GetFollowingAsync(
        string userId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var users = _uow.Repository<ApplicationUser>();

        var items = await users.ListAsync(
            u => u.Followers.Any(f => f.FollowerId == userId),
            UserMappings.ToCardDto(viewerId),
            q => q.OrderBy(u => u.DisplayName),
            page, pageSize, cancellationToken);

        var total = await users.CountAsync(
            u => u.Followers.Any(f => f.FollowerId == userId), cancellationToken);

        return new PagedResult<UserCardDto>(items, total, page, pageSize);
    }
}
