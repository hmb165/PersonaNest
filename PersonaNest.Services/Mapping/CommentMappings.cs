using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>Manual Mapping for <see cref="Comment"/>.</summary>
public static class CommentMappings
{
    /// <summary>
    /// Flat projection - one row per comment, replies included but not yet nested.
    /// <see cref="Implementations.CommentService"/> groups replies under their parent afterwards,
    /// since EF Core cannot translate a self-referencing "replies of my replies" projection.
    /// </summary>
    public static Expression<Func<Comment, CommentDto>> ToDto(string? viewerId) =>
        c => new CommentDto
        {
            Id = c.Id,
            EntryId = c.EntryId,
            ParentCommentId = c.ParentCommentId,
            Content = c.Content,
            AuthorId = c.UserId,
            AuthorUserName = c.User.UserName!,
            AuthorDisplayName = c.User.DisplayName,
            AuthorProfilePictureUrl = c.User.ProfilePictureUrl,
            LikeCount = c.Likes.Count(),
            ViewerHasLiked = viewerId != null && c.Likes.Any(l => l.UserId == viewerId),
            ViewerIsAuthor = viewerId != null && c.UserId == viewerId,
            CreatedAt = c.CreatedAt
        };
}
