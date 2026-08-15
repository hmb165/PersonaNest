using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Implementations;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Tests.Services;

/// <summary>
/// Business-rule tests for <see cref="EntryService"/> (§7, §12, D-11) - rating bounds, enum
/// validity, ownership and the unique (UserId, MediaId) rule. The repository layer is mocked, so
/// these exercise the service's own decisions, not EF Core's query translation.
/// </summary>
public class EntryServiceTests
{
    private static Mock<IUnitOfWork> NewUow(out Mock<IEntryRepository> entries, out Mock<IMediaRepository> media)
    {
        entries = new Mock<IEntryRepository>();
        media = new Mock<IMediaRepository>();

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Entries).Returns(entries.Object);
        uow.SetupGet(u => u.Media).Returns(media.Object);
        return uow;
    }

    [Fact]
    public async Task CreateAsync_RejectsOutOfRangeRating()
    {
        var uow = NewUow(out var entries, out var media);
        media.Setup(m => m.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(true);
        entries.Setup(e => e.ExistsForUserAndMediaAsync("user-1", 1, default)).ReturnsAsync(false);

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());
        var request = new CreateEntryRequest { MediaId = 1, Rating = 10.3m };

        var result = await service.CreateAsync(request, "user-1");

        Assert.False(result.Succeeded);
        Assert.Contains("Rating must be between", result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_RejectsRatingNotOnHalfStep()
    {
        var uow = NewUow(out var entries, out var media);
        media.Setup(m => m.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(true);
        entries.Setup(e => e.ExistsForUserAndMediaAsync("user-1", 1, default)).ReturnsAsync(false);

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());
        var request = new CreateEntryRequest { MediaId = 1, Rating = 7.3m };

        var result = await service.CreateAsync(request, "user-1");

        Assert.False(result.Succeeded);
        Assert.Contains("Rating must be between", result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingMedia()
    {
        var uow = NewUow(out _, out var media);
        media.Setup(m => m.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(false);

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());
        var request = new CreateEntryRequest { MediaId = 999, Rating = 8.0m };

        var result = await service.CreateAsync(request, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That media item no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateEntryForSameUserAndMedia()
    {
        var uow = NewUow(out var entries, out var media);
        media.Setup(m => m.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(true);
        entries.Setup(e => e.ExistsForUserAndMediaAsync("user-1", 1, default)).ReturnsAsync(true);

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());
        var request = new CreateEntryRequest { MediaId = 1, Rating = 8.0m };

        var result = await service.CreateAsync(request, "user-1");

        Assert.False(result.Succeeded);
        Assert.Contains("already logged", result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_RejectsEditingSomeoneElsesEntry()
    {
        var uow = NewUow(out var entries, out _);
        entries.Setup(e => e.GetByIdAsync(42, default))
            .ReturnsAsync(new Entry { Id = 42, UserId = "owner", MediaId = 1 });

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());
        var request = new UpdateEntryRequest
        {
            Id = 42, Rating = 8.0m, Status = EntryStatus.Completed, Privacy = Privacy.Public
        };

        var result = await service.UpdateAsync(request, "someone-else");

        Assert.False(result.Succeeded);
        Assert.Equal("You can only edit your own entries.", result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_RejectsMissingEntry()
    {
        var uow = NewUow(out var entries, out _);
        entries.Setup(e => e.GetByIdAsync(42, default)).ReturnsAsync((Entry?)null);

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());
        var request = new UpdateEntryRequest
        {
            Id = 42, Status = EntryStatus.Completed, Privacy = Privacy.Public
        };

        var result = await service.UpdateAsync(request, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That entry no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUndefinedStatusEnumValue()
    {
        var uow = NewUow(out var entries, out _);
        entries.Setup(e => e.GetByIdAsync(42, default))
            .ReturnsAsync(new Entry { Id = 42, UserId = "user-1", MediaId = 1 });

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());
        var request = new UpdateEntryRequest
        {
            Id = 42, Rating = 8.0m, Status = (EntryStatus)99, Privacy = Privacy.Public
        };

        var result = await service.UpdateAsync(request, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That status value is not valid.", result.FirstError);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUndefinedPrivacyEnumValue()
    {
        var uow = NewUow(out var entries, out _);
        entries.Setup(e => e.GetByIdAsync(42, default))
            .ReturnsAsync(new Entry { Id = 42, UserId = "user-1", MediaId = 1 });

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());
        var request = new UpdateEntryRequest
        {
            Id = 42, Rating = 8.0m, Status = EntryStatus.Completed, Privacy = (Privacy)99
        };

        var result = await service.UpdateAsync(request, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That privacy value is not valid.", result.FirstError);
    }

    [Fact]
    public async Task ToggleLikeAsync_NotifiesEntryOwner_OnlyWhenLiking()
    {
        var uow = NewUow(out var entries, out _);
        entries.Setup(e => e.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Entry, bool>>>(), default))
            .ReturnsAsync(true);

        var likes = new Mock<IRepository<EntryLike>>();
        likes.Setup(l => l.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<EntryLike, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<EntryLike, int?>>>(),
                default))
            .ReturnsAsync((int?)null);
        uow.Setup(u => u.Repository<EntryLike>()).Returns(likes.Object);

        var notifications = new Mock<INotificationService>();
        var service = new EntryService(uow.Object, notifications.Object);

        var result = await service.ToggleLikeAsync("user-1", 42);

        Assert.True(result.Succeeded);
        Assert.True(result.Value);
        notifications.Verify(n => n.NotifyEntryLikedAsync("user-1", 42, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RejectsDeletingSomeoneElsesEntry()
    {
        var uow = NewUow(out var entries, out _);
        entries.Setup(e => e.GetByIdAsync(42, default))
            .ReturnsAsync(new Entry { Id = 42, UserId = "owner", MediaId = 1 });

        var service = new EntryService(uow.Object, Mock.Of<INotificationService>());

        var result = await service.DeleteAsync(42, "someone-else");

        Assert.False(result.Succeeded);
        Assert.Equal("You can only delete your own entries.", result.FirstError);
    }
}
