using System.Linq.Expressions;
using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Implementations;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Tests.Services;

/// <summary>Business-rule tests for <see cref="FollowService"/> (CK_Follow_NotSelf, §19).</summary>
public class FollowServiceTests
{
    [Fact]
    public async Task FollowAsync_RejectsFollowingSelf()
    {
        var uow = new Mock<IUnitOfWork>();
        var service = new FollowService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.FollowAsync("user-1", "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("You cannot follow yourself.", result.FirstError);
    }

    [Fact]
    public async Task FollowAsync_RejectsMissingOrDeletedTarget()
    {
        var users = new Mock<IRepository<ApplicationUser>>();
        users.Setup(u => u.AnyAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), default))
            .ReturnsAsync(false);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<ApplicationUser>()).Returns(users.Object);

        var service = new FollowService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.FollowAsync("user-1", "ghost");

        Assert.False(result.Succeeded);
        Assert.Equal("That account no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task FollowAsync_IsIdempotent_WhenAlreadyFollowing()
    {
        var users = new Mock<IRepository<ApplicationUser>>();
        users.Setup(u => u.AnyAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), default))
            .ReturnsAsync(true);

        var follows = new Mock<IRepository<Follow>>();
        follows.Setup(f => f.AnyAsync(It.IsAny<Expression<Func<Follow, bool>>>(), default))
            .ReturnsAsync(true);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<ApplicationUser>()).Returns(users.Object);
        uow.Setup(u => u.Repository<Follow>()).Returns(follows.Object);

        var service = new FollowService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.FollowAsync("user-1", "user-2");

        Assert.True(result.Succeeded);
        follows.Verify(f => f.AddAsync(It.IsAny<Follow>(), default), Times.Never);
    }

    [Fact]
    public async Task FollowAsync_AddsFollow_WhenNotAlreadyFollowing()
    {
        var users = new Mock<IRepository<ApplicationUser>>();
        users.Setup(u => u.AnyAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), default))
            .ReturnsAsync(true);

        var follows = new Mock<IRepository<Follow>>();
        follows.Setup(f => f.AnyAsync(It.IsAny<Expression<Func<Follow, bool>>>(), default))
            .ReturnsAsync(false);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<ApplicationUser>()).Returns(users.Object);
        uow.Setup(u => u.Repository<Follow>()).Returns(follows.Object);

        var notifications = new Mock<INotificationService>();
        var service = new FollowService(uow.Object, notifications.Object);

        var result = await service.FollowAsync("user-1", "user-2");

        Assert.True(result.Succeeded);
        follows.Verify(f => f.AddAsync(
            It.Is<Follow>(fw => fw.FollowerId == "user-1" && fw.FollowingId == "user-2"), default),
            Times.Once);
        notifications.Verify(
            n => n.NotifyNewFollowerAsync("user-1", "user-2", default), Times.Once);
    }
}
