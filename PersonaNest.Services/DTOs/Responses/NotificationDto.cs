using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Responses;

public sealed record NotificationDto
{
    public int Id { get; init; }
    public NotificationType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Url { get; init; }

    public string? ActorDisplayName { get; init; }
    public string? ActorProfilePictureUrl { get; init; }

    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
