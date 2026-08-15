using Microsoft.EntityFrameworkCore;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(PersonaNestDbContext context) : base(context)
    {
    }

    public Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
        => Set.Where(n => n.RecipientUserId == userId && !n.IsRead)
              .ExecuteUpdateAsync(
                  s => s.SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, DateTime.UtcNow),
                  cancellationToken);
}
