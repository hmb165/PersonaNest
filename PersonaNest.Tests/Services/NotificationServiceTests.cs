using System.Linq.Expressions;
using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Implementations;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Tests.Services;

/// <summary>
/// Business-rule tests for <see cref="NotificationService"/> (Phase 15) - the self-notification
/// skip rules (you never get notified about your own actions) and the read-state operations.
/// </summary>
public class NotificationServiceTests
{
    private static NotificationService NewService(
        out Mock<IUnitOfWork> uow,
        out Mock<INotificationRepository> notifications,
        out Mock<INotificationBroadcaster> broadcaster,
        out Mock<IRepository<ApplicationUser>> users,
        out Mock<IEntryRepository> entries,
        out Mock<IRepository<Comment>> comments)
    {
        notifications = new Mock<INotificationRepository>();
        users = new Mock<IRepository<ApplicationUser>>();
        entries = new Mock<IEntryRepository>();
        comments = new Mock<IRepository<Comment>>();
        broadcaster = new Mock<INotificationBroadcaster>();

        uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Notifications).Returns(notifications.Object);
        uow.Setup(u => u.Repository<ApplicationUser>()).Returns(users.Object);
        uow.SetupGet(u => u.Entries).Returns(entries.Object);
        uow.Setup(u => u.Repository<Comment>()).Returns(comments.Object);

        return new NotificationService(uow.Object, broadcaster.Object);
    }

    [Fact]
    public async Task MarkAsReadAsync_RejectsWhenNotificationBelongsToSomeoneElse()
    {
        var service = NewService(
            out _, out var notifications, out _, out _, out _, out _);
        notifications.Setup(n => n.GetByIdAsync(5, default))
            .ReturnsAsync(new Notification { Id = 5, RecipientUserId = "owner" });

        var result = await service.MarkAsReadAsync(5, "someone-else");

        Assert.False(result.Succeeded);
        notifications.Verify(n => n.Update(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksOwnUnreadNotificationAsRead()
    {
        var service = NewService(
            out var uow, out var notifications, out _, out _, out _, out _);
        var notification = new Notification { Id = 5, RecipientUserId = "user-1", IsRead = false };
        notifications.Setup(n => n.GetByIdAsync(5, default)).ReturnsAsync(notification);

        var result = await service.MarkAsReadAsync(5, "user-1");

        Assert.True(result.Succeeded);
        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAt);
        notifications.Verify(n => n.Update(notification), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_DelegatesToRepository()
    {
        var service = NewService(
            out _, out var notifications, out _, out _, out _, out _);

        var result = await service.MarkAllAsReadAsync("user-1");

        Assert.True(result.Succeeded);
        notifications.Verify(n => n.MarkAllAsReadAsync("user-1", default), Times.Once);
    }

    [Fact]
    public async Task NotifyNewFollowerAsync_SkipsSelfNotification()
    {
        var service = NewService(
            out _, out var notifications, out var broadcaster, out var users, out _, out _);

        await service.NotifyNewFollowerAsync("user-1", "user-1");

        users.Verify(u => u.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
            It.IsAny<Expression<Func<ApplicationUser, NotificationService.ActorProjection>>>(), default),
            Times.Never);
        notifications.Verify(n => n.AddAsync(It.IsAny<Notification>(), default), Times.Never);
        broadcaster.Verify(b => b.BroadcastAsync(
            It.IsAny<string>(), It.IsAny<NotificationDto>(), default), Times.Never);
    }

    [Fact]
    public async Task NotifyNewFollowerAsync_CreatesAndBroadcasts_ForADifferentUser()
    {
        var service = NewService(
            out var uow, out var notifications, out var broadcaster, out var users, out _, out _);

        users.Setup(u => u.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<Expression<Func<ApplicationUser, NotificationService.ActorProjection>>>(),
                default))
            .ReturnsAsync(new NotificationService.ActorProjection("Alice", "alice", null));

        await service.NotifyNewFollowerAsync("actor-1", "recipient-1");

        notifications.Verify(n => n.AddAsync(
            It.Is<Notification>(x =>
                x.RecipientUserId == "recipient-1"
                && x.ActorUserId == "actor-1"
                && x.Message == "Alice started following you."
                && x.Url == "/Profile/alice"),
            default),
            Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
        broadcaster.Verify(b => b.BroadcastAsync(
            "recipient-1", It.Is<NotificationDto>(d => d.Message == "Alice started following you."), default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyEntryLikedAsync_SkipsSelfLike()
    {
        var service = NewService(
            out _, out var notifications, out var broadcaster, out _, out var entries, out _);

        entries.Setup(e => e.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Entry, bool>>>(),
                It.IsAny<Expression<Func<Entry, NotificationService.EntryOwnerProjection>>>(),
                default))
            .ReturnsAsync(new NotificationService.EntryOwnerProjection("user-1", "Some Movie"));

        await service.NotifyEntryLikedAsync("user-1", 42);

        notifications.Verify(n => n.AddAsync(It.IsAny<Notification>(), default), Times.Never);
        broadcaster.Verify(b => b.BroadcastAsync(
            It.IsAny<string>(), It.IsAny<NotificationDto>(), default), Times.Never);
    }

    [Fact]
    public async Task NotifyEntryLikedAsync_NotifiesTheOwner_WhenSomeoneElseLikes()
    {
        var service = NewService(
            out _, out var notifications, out var broadcaster, out var users, out var entries, out _);

        entries.Setup(e => e.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Entry, bool>>>(),
                It.IsAny<Expression<Func<Entry, NotificationService.EntryOwnerProjection>>>(),
                default))
            .ReturnsAsync(new NotificationService.EntryOwnerProjection("owner-1", "Some Movie"));

        users.Setup(u => u.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<Expression<Func<ApplicationUser, NotificationService.ActorProjection>>>(),
                default))
            .ReturnsAsync(new NotificationService.ActorProjection("Bob", "bob", null));

        await service.NotifyEntryLikedAsync("actor-1", 42);

        notifications.Verify(n => n.AddAsync(
            It.Is<Notification>(x =>
                x.RecipientUserId == "owner-1"
                && x.Message == "Bob liked your entry for Some Movie."
                && x.Url == "/Entries/Details/42"),
            default),
            Times.Once);
        broadcaster.Verify(b => b.BroadcastAsync("owner-1", It.IsAny<NotificationDto>(), default), Times.Once);
    }

    [Fact]
    public async Task NotifyNewReplyAsync_SkipsWhenReplyingToOwnComment()
    {
        var service = NewService(
            out _, out var notifications, out var broadcaster, out _, out _, out var comments);

        comments.Setup(c => c.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Comment, bool>>>(),
                It.IsAny<Expression<Func<Comment, NotificationService.CommentOwnerProjection>>>(),
                default))
            .ReturnsAsync(new NotificationService.CommentOwnerProjection("user-1", 7));

        await service.NotifyNewReplyAsync("user-1", 10);

        notifications.Verify(n => n.AddAsync(It.IsAny<Notification>(), default), Times.Never);
        broadcaster.Verify(b => b.BroadcastAsync(
            It.IsAny<string>(), It.IsAny<NotificationDto>(), default), Times.Never);
    }
}
