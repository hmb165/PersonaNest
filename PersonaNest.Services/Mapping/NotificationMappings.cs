using System.Linq.Expressions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Mapping;

/// <summary>Manual Mapping for <see cref="Notification"/>.</summary>
public static class NotificationMappings
{
    public static Expression<Func<Notification, NotificationDto>> ToDto =>
        n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Message = n.Message,
            Url = n.Url,
            ActorDisplayName = n.Actor != null ? n.Actor.DisplayName : null,
            ActorProfilePictureUrl = n.Actor != null ? n.Actor.ProfilePictureUrl : null,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
}
