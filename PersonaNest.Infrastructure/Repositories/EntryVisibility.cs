using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;

namespace PersonaNest.Infrastructure.Repositories;

/// <summary>
/// The §18 privacy rule as an expression, so SQL Server evaluates it rather than the web server.
/// <para>
/// This lives in Infrastructure because it must be translatable to SQL - filtering in memory
/// would mean reading every entry before discarding most of them, which §13 forbids. The
/// <em>policy</em> it encodes is owned by the Services layer; Phase 9 calls into these queries
/// rather than re-implementing the rule.
/// </para>
/// </summary>
internal static class EntryVisibility
{
    /// <summary>
    /// Entries <paramref name="viewerId"/> may see. Pass null for an anonymous visitor.
    /// <list type="bullet">
    ///   <item>Public - everyone.</item>
    ///   <item>FollowersOnly - the owner, and anyone who follows the owner.</item>
    ///   <item>Private - the owner only.</item>
    /// </list>
    /// </summary>
    public static Expression<Func<Entry, bool>> For(string? viewerId)
    {
        if (string.IsNullOrEmpty(viewerId))
        {
            return e => e.Privacy == Privacy.Public;
        }

        return e =>
            e.Privacy == Privacy.Public
            || e.UserId == viewerId
            || (e.Privacy == Privacy.FollowersOnly
                && e.User.Followers.Any(f => f.FollowerId == viewerId));
    }
}
