using System.Linq.Expressions;
using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Implementations;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Tests.Services;

/// <summary>Business-rule tests for <see cref="CommentService"/> (§18, D-17 - one level of replies).</summary>
public class CommentServiceTests
{
    private static (Mock<IUnitOfWork> Uow, Mock<IEntryRepository> Entries, Mock<IRepository<Comment>> Comments) NewUow()
    {
        var entries = new Mock<IEntryRepository>();
        var comments = new Mock<IRepository<Comment>>();
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Entries).Returns(entries.Object);
        uow.Setup(u => u.Repository<Comment>()).Returns(comments.Object);
        return (uow, entries, comments);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingEntry()
    {
        var (uow, entries, _) = NewUow();
        entries.Setup(e => e.AnyAsync(It.IsAny<Expression<Func<Entry, bool>>>(), default))
            .ReturnsAsync(false);

        var service = new CommentService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.CreateAsync(
            new CreateCommentRequest { EntryId = 999, Content = "Great!" }, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That entry no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_RejectsReplyingToAReply()
    {
        var (uow, entries, comments) = NewUow();
        entries.Setup(e => e.AnyAsync(It.IsAny<Expression<Func<Entry, bool>>>(), default))
            .ReturnsAsync(true);

        // The parent comment is itself already a reply (has its own ParentCommentId).
        comments.Setup(c => c.GetByIdAsync(10, default))
            .ReturnsAsync(new Comment { Id = 10, EntryId = 1, ParentCommentId = 3 });

        var service = new CommentService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.CreateAsync(
            new CreateCommentRequest { EntryId = 1, ParentCommentId = 10, Content = "Re-reply" }, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("Replies can only be one level deep.", result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_RejectsReplyToACommentOnADifferentEntry()
    {
        var (uow, entries, comments) = NewUow();
        entries.Setup(e => e.AnyAsync(It.IsAny<Expression<Func<Entry, bool>>>(), default))
            .ReturnsAsync(true);

        // Parent belongs to entry 2, but the reply is being posted against entry 1.
        comments.Setup(c => c.GetByIdAsync(10, default))
            .ReturnsAsync(new Comment { Id = 10, EntryId = 2, ParentCommentId = null });

        var service = new CommentService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.CreateAsync(
            new CreateCommentRequest { EntryId = 1, ParentCommentId = 10, Content = "Reply" }, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That comment no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_AllowsOneLevelReply()
    {
        var (uow, entries, comments) = NewUow();
        entries.Setup(e => e.AnyAsync(It.IsAny<Expression<Func<Entry, bool>>>(), default))
            .ReturnsAsync(true);
        comments.Setup(c => c.GetByIdAsync(10, default))
            .ReturnsAsync(new Comment { Id = 10, EntryId = 1, ParentCommentId = null });

        var notifications = new Mock<INotificationService>();
        var service = new CommentService(uow.Object, notifications.Object);

        var result = await service.CreateAsync(
            new CreateCommentRequest { EntryId = 1, ParentCommentId = 10, Content = "Reply" }, "user-1");

        Assert.True(result.Succeeded);
        comments.Verify(c => c.AddAsync(
            It.Is<Comment>(cm => cm.ParentCommentId == 10 && cm.EntryId == 1), default), Times.Once);

        // A reply notifies the parent comment's author, not the entry owner - no double notification.
        notifications.Verify(n => n.NotifyNewReplyAsync("user-1", 10, default), Times.Once);
        notifications.Verify(
            n => n.NotifyNewCommentAsync(It.IsAny<string>(), It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NotifiesEntryOwner_ForATopLevelComment()
    {
        var (uow, entries, comments) = NewUow();
        entries.Setup(e => e.AnyAsync(It.IsAny<Expression<Func<Entry, bool>>>(), default))
            .ReturnsAsync(true);

        var notifications = new Mock<INotificationService>();
        var service = new CommentService(uow.Object, notifications.Object);

        var result = await service.CreateAsync(
            new CreateCommentRequest { EntryId = 1, Content = "Nice write-up!" }, "user-1");

        Assert.True(result.Succeeded);
        notifications.Verify(n => n.NotifyNewCommentAsync("user-1", 1, default), Times.Once);
        notifications.Verify(
            n => n.NotifyNewReplyAsync(It.IsAny<string>(), It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RejectsDeletingSomeoneElsesComment()
    {
        var (uow, _, comments) = NewUow();
        comments.Setup(c => c.GetByIdAsync(1, default))
            .ReturnsAsync(new Comment { Id = 1, UserId = "owner", EntryId = 1 });

        var service = new CommentService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.DeleteAsync(1, "someone-else");

        Assert.False(result.Succeeded);
        Assert.Equal("You can only delete your own comments.", result.FirstError);
    }

    [Fact]
    public async Task ToggleLikeAsync_RejectsMissingComment()
    {
        var (uow, _, comments) = NewUow();
        comments.Setup(c => c.AnyAsync(It.IsAny<Expression<Func<Comment, bool>>>(), default))
            .ReturnsAsync(false);

        var service = new CommentService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.ToggleLikeAsync("user-1", 999);

        Assert.False(result.Succeeded);
        Assert.Equal("That comment no longer exists.", result.FirstError);
    }
}
