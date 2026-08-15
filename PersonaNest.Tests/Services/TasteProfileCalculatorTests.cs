using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Implementations;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Tests.Services;

/// <summary>
/// Tests the actual taste-profile arithmetic (§22) - category percentages, average rating, most
/// active month, top-tag ranking - not just that the method runs. The repository is mocked to
/// return canned <see cref="TasteProfileCalculator.TasteEntryRow"/> rows (internal, visible to
/// this test project only) so these exercise the calculator's own logic, independent of EF Core.
/// </summary>
public class TasteProfileCalculatorTests
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    private static TasteProfileCalculator NewCalculator(IUnitOfWork uow) =>
        new(uow, Mock.Of<IAiNarrativeGenerator>(), EmptyConfiguration);

    private static Mock<IUnitOfWork> NewUowReturning(
        IReadOnlyList<TasteProfileCalculator.TasteEntryRow> rows, out Mock<IEntryRepository> entries)
    {
        entries = new Mock<IEntryRepository>();
        entries.Setup(e => e.ListAsync(
                It.IsAny<Expression<Func<Entry, bool>>>(),
                It.IsAny<Expression<Func<Entry, TasteProfileCalculator.TasteEntryRow>>>(),
                It.IsAny<Func<IQueryable<Entry>, IOrderedQueryable<Entry>>>(),
                1, 100, default))
            .ReturnsAsync(rows);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Entries).Returns(entries.Object);
        return uow;
    }

    private static TasteProfileCalculator.TasteEntryRow Row(
        int categoryId, string categoryName, decimal? rating, DateTime createdAt,
        string? review = null, params TagDto[] tags) => new()
    {
        CategoryId = categoryId,
        CategoryName = categoryName,
        CategoryColorToken = "accent",
        CategoryIcon = "🎬",
        Rating = rating,
        Review = review,
        CreatedAt = createdAt,
        Tags = tags.ToList()
    };

    [Fact]
    public async Task ComputeAsync_ReturnsNull_WhenUserHasNoEntries()
    {
        var uow = NewUowReturning(Array.Empty<TasteProfileCalculator.TasteEntryRow>(), out _);
        var calculator = NewCalculator(uow.Object);

        var result = await calculator.ComputeAsync("user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task ComputeAsync_ComputesCategoryPercentagesAcrossMultipleCategories()
    {
        var rows = new List<TasteProfileCalculator.TasteEntryRow>
        {
            Row(1, "Games", 9m, new DateTime(2026, 1, 1)),
            Row(1, "Games", 7m, new DateTime(2026, 1, 5)),
            Row(1, "Games", 8m, new DateTime(2026, 1, 10)),
            Row(2, "Movies", 10m, new DateTime(2026, 2, 1))
        };
        var uow = NewUowReturning(rows, out _);
        var calculator = NewCalculator(uow.Object);

        var result = await calculator.ComputeAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal(4, result!.TotalEntries);

        var games = Assert.Single(result.Categories, c => c.CategoryName == "Games");
        Assert.Equal(3, games.EntryCount);
        Assert.Equal(75.0m, games.Percentage); // 3 of 4 entries

        var movies = Assert.Single(result.Categories, c => c.CategoryName == "Movies");
        Assert.Equal(1, movies.EntryCount);
        Assert.Equal(25.0m, movies.Percentage);

        // Categories ordered by entry count, most-logged first.
        Assert.Equal("Games", result.Categories[0].CategoryName);
    }

    [Fact]
    public async Task ComputeAsync_AveragesOnlyRatedEntries()
    {
        var rows = new List<TasteProfileCalculator.TasteEntryRow>
        {
            Row(1, "Games", 8m, new DateTime(2026, 1, 1)),
            Row(1, "Games", 10m, new DateTime(2026, 1, 2)),
            Row(1, "Games", null, new DateTime(2026, 1, 3)) // unrated - must not skew the average
        };
        var uow = NewUowReturning(rows, out _);
        var calculator = NewCalculator(uow.Object);

        var result = await calculator.ComputeAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal(9.0m, result!.AverageRating); // (8 + 10) / 2, not / 3
        Assert.Equal(3, result.TotalEntries);
    }

    [Fact]
    public async Task ComputeAsync_CountsOnlyEntriesWithAReview()
    {
        var rows = new List<TasteProfileCalculator.TasteEntryRow>
        {
            Row(1, "Games", 8m, new DateTime(2026, 1, 1), review: "Loved it"),
            Row(1, "Games", 7m, new DateTime(2026, 1, 2), review: ""),
            Row(1, "Games", 6m, new DateTime(2026, 1, 3), review: null)
        };
        var uow = NewUowReturning(rows, out _);
        var calculator = NewCalculator(uow.Object);

        var result = await calculator.ComputeAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalReviews);
    }

    [Fact]
    public async Task ComputeAsync_RanksTopTagsByUseCountDescending()
    {
        var rpg = new TagDto { Id = 1, Name = "RPG" };
        var indie = new TagDto { Id = 2, Name = "Indie" };

        var rows = new List<TasteProfileCalculator.TasteEntryRow>
        {
            Row(1, "Games", 8m, new DateTime(2026, 1, 1), tags: new[] { rpg }),
            Row(1, "Games", 7m, new DateTime(2026, 1, 2), tags: new[] { rpg, indie }),
            Row(1, "Games", 9m, new DateTime(2026, 1, 3), tags: new[] { rpg })
        };
        var uow = NewUowReturning(rows, out _);
        var calculator = NewCalculator(uow.Object);

        var result = await calculator.ComputeAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal(2, result!.TopTags.Count);
        Assert.Equal("RPG", result.TopTags[0].Name);
        Assert.Equal(3, result.TopTags[0].UseCount);
        Assert.Equal("Indie", result.TopTags[1].Name);
        Assert.Equal(1, result.TopTags[1].UseCount);
    }

    [Fact]
    public async Task ComputeAsync_PicksTheMonthWithTheMostEntriesAsMostActive()
    {
        var rows = new List<TasteProfileCalculator.TasteEntryRow>
        {
            Row(1, "Games", 8m, new DateTime(2026, 1, 5)),
            Row(1, "Games", 7m, new DateTime(2026, 2, 1)),
            Row(1, "Games", 9m, new DateTime(2026, 2, 15)),
            Row(1, "Games", 6m, new DateTime(2026, 2, 20))
        };
        var uow = NewUowReturning(rows, out _);
        var calculator = NewCalculator(uow.Object);

        var result = await calculator.ComputeAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal(new DateTime(2026, 2, 1), result!.MostActiveMonth);
    }

    [Fact]
    public async Task ComputeAsync_PagesThroughMoreThanOneHundredEntries()
    {
        // Repository<T>.ListAsync silently clamps pageSize to 100 (Phase 13 finding) - this
        // proves the calculator now pages through all of a prolific user's entries instead of
        // silently dropping everything past the first 100.
        var rows = Enumerable.Range(0, 150)
            .Select(i => Row(1, "Games", 8m, new DateTime(2026, 1, 1).AddDays(i)))
            .ToList();

        var entries = new Mock<IEntryRepository>();
        entries.Setup(e => e.ListAsync(
                It.IsAny<Expression<Func<Entry, bool>>>(),
                It.IsAny<Expression<Func<Entry, TasteProfileCalculator.TasteEntryRow>>>(),
                It.IsAny<Func<IQueryable<Entry>, IOrderedQueryable<Entry>>>(),
                1, 100, default))
            .ReturnsAsync(rows.Take(100).ToList());
        entries.Setup(e => e.ListAsync(
                It.IsAny<Expression<Func<Entry, bool>>>(),
                It.IsAny<Expression<Func<Entry, TasteProfileCalculator.TasteEntryRow>>>(),
                It.IsAny<Func<IQueryable<Entry>, IOrderedQueryable<Entry>>>(),
                2, 100, default))
            .ReturnsAsync(rows.Skip(100).ToList());

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Entries).Returns(entries.Object);
        var calculator = NewCalculator(uow.Object);

        var result = await calculator.ComputeAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal(150, result!.TotalEntries);
    }

    // ─── RefreshNarrativeAsync (bonus: AI) ───────────────────────────────────────────────

    [Fact]
    public async Task RefreshNarrativeAsync_NoOp_WhenNoProfileExists()
    {
        var profiles = new Mock<IRepository<TasteProfile>>();
        profiles.Setup(p => p.GetByIdAsync("user-1", default)).ReturnsAsync((TasteProfile?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<TasteProfile>()).Returns(profiles.Object);

        var generator = new Mock<IAiNarrativeGenerator>();
        var calculator = new TasteProfileCalculator(uow.Object, generator.Object, EmptyConfiguration);

        await calculator.RefreshNarrativeAsync("user-1");

        generator.Verify(g => g.GenerateAsync(
            It.IsAny<string>(), It.IsAny<TasteProfileDto>(), default), Times.Never);
    }

    [Fact]
    public async Task RefreshNarrativeAsync_NoOp_WhenExistingNarrativeIsStillFresh()
    {
        var profiles = new Mock<IRepository<TasteProfile>>();
        profiles.Setup(p => p.GetByIdAsync("user-1", default)).ReturnsAsync(
            new TasteProfile { UserId = "user-1", AiNarrativeGeneratedAt = DateTime.UtcNow });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<TasteProfile>()).Returns(profiles.Object);

        var generator = new Mock<IAiNarrativeGenerator>();
        // Default 24h freshness window (EmptyConfiguration) - "just now" is well within it.
        var calculator = new TasteProfileCalculator(uow.Object, generator.Object, EmptyConfiguration);

        await calculator.RefreshNarrativeAsync("user-1");

        generator.Verify(g => g.GenerateAsync(
            It.IsAny<string>(), It.IsAny<TasteProfileDto>(), default), Times.Never);
    }

    [Fact]
    public async Task RefreshNarrativeAsync_NoOp_WhenGeneratorReturnsNull()
    {
        var tracked = new TasteProfile { UserId = "user-1", AiNarrativeGeneratedAt = null };

        var profiles = new Mock<IRepository<TasteProfile>>();
        profiles.Setup(p => p.GetByIdAsync("user-1", default)).ReturnsAsync(tracked);
        profiles.Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<TasteProfile, bool>>>(),
                It.IsAny<Expression<Func<TasteProfile, TasteProfileDto>>>(), default))
            .ReturnsAsync(new TasteProfileDto { TotalEntries = 5 });

        var users = new Mock<IRepository<ApplicationUser>>();
        users.Setup(u => u.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<Expression<Func<ApplicationUser, string>>>(), default))
            .ReturnsAsync("Alice");

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<TasteProfile>()).Returns(profiles.Object);
        uow.Setup(u => u.Repository<ApplicationUser>()).Returns(users.Object);

        var generator = new Mock<IAiNarrativeGenerator>();
        generator.Setup(g => g.GenerateAsync("Alice", It.IsAny<TasteProfileDto>(), default))
            .ReturnsAsync((string?)null);

        var calculator = new TasteProfileCalculator(uow.Object, generator.Object, EmptyConfiguration);
        await calculator.RefreshNarrativeAsync("user-1");

        profiles.Verify(p => p.Update(It.IsAny<TasteProfile>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task RefreshNarrativeAsync_PersistsNarrative_WhenGeneratorSucceeds()
    {
        var tracked = new TasteProfile { UserId = "user-1", AiNarrativeGeneratedAt = null };

        var profiles = new Mock<IRepository<TasteProfile>>();
        profiles.Setup(p => p.GetByIdAsync("user-1", default)).ReturnsAsync(tracked);
        profiles.Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<TasteProfile, bool>>>(),
                It.IsAny<Expression<Func<TasteProfile, TasteProfileDto>>>(), default))
            .ReturnsAsync(new TasteProfileDto { TotalEntries = 5 });

        var users = new Mock<IRepository<ApplicationUser>>();
        users.Setup(u => u.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<Expression<Func<ApplicationUser, string>>>(), default))
            .ReturnsAsync("Alice");

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<TasteProfile>()).Returns(profiles.Object);
        uow.Setup(u => u.Repository<ApplicationUser>()).Returns(users.Object);

        var generator = new Mock<IAiNarrativeGenerator>();
        generator.Setup(g => g.GenerateAsync("Alice", It.IsAny<TasteProfileDto>(), default))
            .ReturnsAsync("Alice loves story-driven RPGs.");

        var calculator = new TasteProfileCalculator(uow.Object, generator.Object, EmptyConfiguration);
        await calculator.RefreshNarrativeAsync("user-1");

        Assert.Equal("Alice loves story-driven RPGs.", tracked.AiNarrative);
        Assert.NotNull(tracked.AiNarrativeGeneratedAt);
        profiles.Verify(p => p.Update(tracked), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
