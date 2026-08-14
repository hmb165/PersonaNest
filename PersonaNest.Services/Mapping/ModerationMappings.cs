using System.Linq.Expressions;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>Manual Mapping for the moderation surfaces.</summary>
public static class ModerationMappings
{
    public static Expression<Func<ModeratorApplication, ModeratorApplicationDto>> ToApplicationDto =>
        a => new ModeratorApplicationDto
        {
            Id = a.Id,
            UserId = a.UserId,
            UserName = a.User.UserName!,
            DisplayName = a.User.DisplayName,
            ProfilePictureUrl = a.User.ProfilePictureUrl,
            ApplicantEntryCount = a.User.Entries.Count(),
            ApplicantJoinedAt = a.User.CreatedAt,
            Reason = a.Reason,
            RelevantExperience = a.RelevantExperience,
            Status = a.Status,
            AppliedAt = a.AppliedAt,
            ReviewedByAdminUserName = a.ReviewedByAdmin != null
                ? a.ReviewedByAdmin.UserName
                : null,
            ReviewedAt = a.ReviewedAt,
            AdminNotes = a.AdminNotes
        };

    /// <summary>
    /// Domain read model -&gt; DTO. In-memory rather than an expression: the repository has
    /// already merged three tables and materialised the page (Specification v3 §6).
    /// </summary>
    public static ReportQueueItemDto ToQueueItemDto(this ReportQueueItem item) => new()
    {
        Id = item.Id,
        TargetType = item.TargetType,
        TargetId = item.TargetId,
        TargetLabel = item.TargetLabel,
        TargetSnippet = Truncate(item.TargetSnippet, 140),
        ReporterUserName = item.ReporterUserName,
        Reason = item.Reason,
        Status = item.Status,
        CreatedAt = item.CreatedAt
    };

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max] + "\u2026";
    }
}
