using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="ICommentService"/>
public class CommentService : ICommentService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public CommentService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    private const int PageSize = 100;

    public async Task<IReadOnlyList<CommentDto>> GetForEntryAsync(
        int entryId, string? viewerId, CancellationToken cancellationToken = default)
    {
        // Repository<T>.ListAsync silently clamps pageSize to MaxPageSize (100), so the previous
        // "pageSize: 500" call was actually only ever reading an entry's first 100 comments -
        // an active discussion thread could silently lose comments/replies past that (Phase 13
        // finding). Pages through everything explicitly instead.
        var flat = new List<CommentDto>();
        var page = 1;
        while (true)
        {
            var batch = await _uow.Repository<Comment>().ListAsync(
                c => c.EntryId == entryId,
                CommentMappings.ToDto(viewerId),
                q => q.OrderBy(c => c.CreatedAt),
                page, PageSize, cancellationToken);

            flat.AddRange(batch);
            if (batch.Count < PageSize)
            {
                break;
            }

            page++;
        }

        var repliesByParent = flat
            .Where(c => c.ParentCommentId is not null)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CommentDto>)g.ToList());

        return flat
            .Where(c => c.ParentCommentId is null)
            .Select(c => c with
            {
                Replies = repliesByParent.TryGetValue(c.Id, out var replies)
                    ? replies
                    : Array.Empty<CommentDto>()
            })
            .ToList();
    }

    public async Task<ServiceResult<int>> CreateAsync(
        CreateCommentRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await _uow.Entries.AnyAsync(e => e.Id == request.EntryId, cancellationToken))
        {
            return ServiceResult<int>.Failure("That entry no longer exists.");
        }

        if (request.ParentCommentId is { } parentId)
        {
            var parent = await _uow.Repository<Comment>().GetByIdAsync(parentId, cancellationToken);

            if (parent is null || parent.EntryId != request.EntryId)
            {
                return ServiceResult<int>.Failure("That comment no longer exists.");
            }

            // One level of nesting only (decision D-17): a reply cannot itself be replied to.
            if (parent.ParentCommentId is not null)
            {
                return ServiceResult<int>.Failure("Replies can only be one level deep.");
            }
        }

        var comment = new Comment
        {
            EntryId = request.EntryId,
            UserId = userId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Repository<Comment>().AddAsync(comment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (request.ParentCommentId is { } replyToId)
        {
            await _notificationService.NotifyNewReplyAsync(userId, replyToId, cancellationToken);
        }
        else
        {
            await _notificationService.NotifyNewCommentAsync(userId, request.EntryId, cancellationToken);
        }

        return ServiceResult<int>.Success(comment.Id);
    }

    public async Task<ServiceResult> DeleteAsync(
        int commentId, string userId, CancellationToken cancellationToken = default)
    {
        var repository = _uow.Repository<Comment>();
        var comment = await repository.GetByIdAsync(commentId, cancellationToken);

        if (comment is null)
        {
            return ServiceResult.Failure("That comment no longer exists.");
        }

        if (comment.UserId != userId)
        {
            return ServiceResult.Failure("You can only delete your own comments.");
        }

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.DeletedById = userId;
        repository.Update(comment);

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<bool>> ToggleLikeAsync(
        string userId, int commentId, CancellationToken cancellationToken = default)
    {
        if (!await _uow.Repository<Comment>().AnyAsync(c => c.Id == commentId, cancellationToken))
        {
            return ServiceResult<bool>.Failure("That comment no longer exists.");
        }

        var repository = _uow.Repository<CommentLike>();

        var existingId = await repository.FirstOrDefaultAsync(
            l => l.UserId == userId && l.CommentId == commentId,
            l => (int?)l.Id,
            cancellationToken);

        if (existingId is { } id)
        {
            var tracked = await repository.GetByIdAsync(id, cancellationToken);
            if (tracked is not null)
            {
                repository.Remove(tracked);
                await _uow.SaveChangesAsync(cancellationToken);
            }

            return ServiceResult<bool>.Success(false);
        }

        await repository.AddAsync(new CommentLike
        {
            CommentId = commentId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }
}
