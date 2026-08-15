using System.Linq.Expressions;
using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Implementations;

namespace PersonaNest.Tests.Services;

/// <summary>Business-rule tests for <see cref="FavoriteService"/> (§19).</summary>
public class FavoriteServiceTests
{
    [Fact]
    public async Task ToggleAsync_RejectsMissingMedia()
    {
        var media = new Mock<IMediaRepository>();
        media.Setup(m => m.AnyAsync(It.IsAny<Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(false);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Media).Returns(media.Object);

        var service = new FavoriteService(uow.Object);

        var result = await service.ToggleAsync("user-1", 999);

        Assert.False(result.Succeeded);
        Assert.Equal("That media item no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task ToggleAsync_AddsFavorite_WhenNotAlreadyFavorited()
    {
        var media = new Mock<IMediaRepository>();
        media.Setup(m => m.AnyAsync(It.IsAny<Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(true);

        var favorites = new Mock<IRepository<Favorite>>();
        favorites.Setup(f => f.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Favorite, bool>>>(),
                It.IsAny<Expression<Func<Favorite, int?>>>(),
                default))
            .ReturnsAsync((int?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Media).Returns(media.Object);
        uow.Setup(u => u.Repository<Favorite>()).Returns(favorites.Object);

        var service = new FavoriteService(uow.Object);

        var result = await service.ToggleAsync("user-1", 1);

        Assert.True(result.Succeeded);
        Assert.True(result.Value);
        favorites.Verify(f => f.AddAsync(
            It.Is<Favorite>(fav => fav.UserId == "user-1" && fav.MediaId == 1), default), Times.Once);
    }

    [Fact]
    public async Task ToggleAsync_RemovesFavorite_WhenAlreadyFavorited()
    {
        var media = new Mock<IMediaRepository>();
        media.Setup(m => m.AnyAsync(It.IsAny<Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(true);

        var existing = new Favorite { Id = 5, UserId = "user-1", MediaId = 1 };

        var favorites = new Mock<IRepository<Favorite>>();
        favorites.Setup(f => f.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Favorite, bool>>>(),
                It.IsAny<Expression<Func<Favorite, int?>>>(),
                default))
            .ReturnsAsync((int?)5);
        favorites.Setup(f => f.GetByIdAsync(5, default)).ReturnsAsync(existing);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Media).Returns(media.Object);
        uow.Setup(u => u.Repository<Favorite>()).Returns(favorites.Object);

        var service = new FavoriteService(uow.Object);

        var result = await service.ToggleAsync("user-1", 1);

        Assert.True(result.Succeeded);
        Assert.False(result.Value);
        favorites.Verify(f => f.Remove(existing), Times.Once);
    }
}
