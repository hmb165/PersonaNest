using System.Linq.Expressions;
using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Implementations;

namespace PersonaNest.Tests.Services;

/// <summary>Business-rule tests for <see cref="CollectionService"/> (§20).</summary>
public class CollectionServiceTests
{
    private static (Mock<IUnitOfWork> Uow, Mock<IRepository<Collection>> Collections) NewUow()
    {
        var collections = new Mock<IRepository<Collection>>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<Collection>()).Returns(collections.Object);
        return (uow, collections);
    }

    [Fact]
    public async Task CreateAsync_RejectsUndefinedPrivacyEnumValue()
    {
        var (uow, _) = NewUow();
        var service = new CollectionService(uow.Object);

        var result = await service.CreateAsync(
            new CreateCollectionRequest { Name = "Favorites", Privacy = (Privacy)99 }, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That privacy value is not valid.", result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_RejectsEditingSomeoneElsesCollection()
    {
        var (uow, collections) = NewUow();
        collections.Setup(c => c.GetByIdAsync(1, default))
            .ReturnsAsync(new Collection { Id = 1, UserId = "owner", Name = "Old" });

        var service = new CollectionService(uow.Object);

        var result = await service.UpdateAsync(
            new UpdateCollectionRequest { Id = 1, Name = "New", Privacy = Privacy.Public },
            "someone-else");

        Assert.False(result.Succeeded);
        Assert.Equal("You can only edit your own collections.", result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUndefinedPrivacyEnumValue_EvenForOwner()
    {
        var (uow, collections) = NewUow();
        collections.Setup(c => c.GetByIdAsync(1, default))
            .ReturnsAsync(new Collection { Id = 1, UserId = "user-1", Name = "Old" });

        var service = new CollectionService(uow.Object);

        var result = await service.UpdateAsync(
            new UpdateCollectionRequest { Id = 1, Name = "New", Privacy = (Privacy)99 }, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That privacy value is not valid.", result.FirstError);
    }

    [Fact]
    public async Task DeleteAsync_RejectsDeletingSomeoneElsesCollection()
    {
        var (uow, collections) = NewUow();
        collections.Setup(c => c.GetByIdAsync(1, default))
            .ReturnsAsync(new Collection { Id = 1, UserId = "owner", Name = "Mine" });

        var service = new CollectionService(uow.Object);

        var result = await service.DeleteAsync(1, "someone-else");

        Assert.False(result.Succeeded);
        Assert.Equal("You can only delete your own collections.", result.FirstError);
    }

    [Fact]
    public async Task AddItemAsync_RejectsMissingMedia()
    {
        var (uow, collections) = NewUow();
        collections.Setup(c => c.GetByIdAsync(1, default))
            .ReturnsAsync(new Collection { Id = 1, UserId = "user-1", Name = "Mine" });

        var media = new Mock<IMediaRepository>();
        media.Setup(m => m.AnyAsync(It.IsAny<Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(false);
        uow.SetupGet(u => u.Media).Returns(media.Object);

        var service = new CollectionService(uow.Object);

        var result = await service.AddItemAsync(
            new AddCollectionItemRequest { CollectionId = 1, MediaId = 999 }, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That media item no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task AddItemAsync_RejectsDuplicateItem()
    {
        var (uow, collections) = NewUow();
        collections.Setup(c => c.GetByIdAsync(1, default))
            .ReturnsAsync(new Collection { Id = 1, UserId = "user-1", Name = "Mine" });

        var media = new Mock<IMediaRepository>();
        media.Setup(m => m.AnyAsync(It.IsAny<Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(true);
        uow.SetupGet(u => u.Media).Returns(media.Object);

        var items = new Mock<IRepository<CollectionItem>>();
        items.Setup(i => i.AnyAsync(It.IsAny<Expression<Func<CollectionItem, bool>>>(), default))
            .ReturnsAsync(true);
        uow.Setup(u => u.Repository<CollectionItem>()).Returns(items.Object);

        var service = new CollectionService(uow.Object);

        var result = await service.AddItemAsync(
            new AddCollectionItemRequest { CollectionId = 1, MediaId = 5 }, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That item is already in this collection.", result.FirstError);
    }

    [Fact]
    public async Task RemoveItemAsync_RejectsChangingSomeoneElsesCollection()
    {
        var (uow, collections) = NewUow();
        collections.Setup(c => c.GetByIdAsync(1, default))
            .ReturnsAsync(new Collection { Id = 1, UserId = "owner", Name = "Mine" });

        var service = new CollectionService(uow.Object);

        var result = await service.RemoveItemAsync(1, 5, "someone-else");

        Assert.False(result.Succeeded);
        Assert.Equal("You can only change your own collections.", result.FirstError);
    }
}
