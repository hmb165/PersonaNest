using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Comments on an Entry, with one level of replies (§5, §18).</summary>
public interface ICommentService
{
    /// <summary>
    /// Top-level comments for an entry, each with its replies attached. A comment thread is
    /// small enough that this is not paginated.
    /// </summary>
    Task<IReadOnlyList<CommentDto>> GetForEntryAsync(
        int entryId, string? viewerId, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(
        CreateCommentRequest request, string userId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the comment. Its replies, if any, stop being reachable (§5, global query filter).</summary>
    Task<ServiceResult> DeleteAsync(
        int commentId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Adds or removes the viewer's like on a comment. Returns the state after the toggle.</summary>
    Task<ServiceResult<bool>> ToggleLikeAsync(
        string userId, int commentId, CancellationToken cancellationToken = default);
}
