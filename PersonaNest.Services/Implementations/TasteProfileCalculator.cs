using Microsoft.Extensions.Configuration;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="ITasteProfileCalculator"/>
public class TasteProfileCalculator : ITasteProfileCalculator
{
    private readonly IUnitOfWork _uow;
    private readonly IAiNarrativeGenerator _narrativeGenerator;
    private readonly TimeSpan _narrativeRefreshInterval;

    public TasteProfileCalculator(
        IUnitOfWork uow, IAiNarrativeGenerator narrativeGenerator, IConfiguration configuration)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _narrativeGenerator = narrativeGenerator ?? throw new ArgumentNullException(nameof(narrativeGenerator));

        var hours = configuration.GetValue<int?>("Ai:NarrativeRefreshIntervalHours") ?? 24;
        _narrativeRefreshInterval = TimeSpan.FromHours(Math.Max(1, hours));
    }

    public Task<TasteProfileDto?> ComputeAsync(
        string userId, CancellationToken cancellationToken = default)
        => ComputeCoreAsync(userId, cancellationToken);

    public async Task<bool> RefreshAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var computed = await ComputeCoreAsync(userId, cancellationToken);

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // Clear the old Categories/Tags first and flush, so the fresh rows inserted below
            // never collide with the (TasteProfileId, CategoryId/TagId) unique indexes.
            var oldCategories = await _uow.Repository<TasteProfileCategory>().ListAsync(
                tc => tc.TasteProfileId == userId, tc => tc,
                q => q.OrderBy(tc => tc.Id), page: 1, pageSize: 100, cancellationToken);
            var oldTags = await _uow.Repository<TasteProfileTag>().ListAsync(
                tt => tt.TasteProfileId == userId, tt => tt,
                q => q.OrderBy(tt => tt.Id), page: 1, pageSize: 100, cancellationToken);

            if (oldCategories.Count > 0)
            {
                _uow.Repository<TasteProfileCategory>().RemoveRange(oldCategories);
            }

            if (oldTags.Count > 0)
            {
                _uow.Repository<TasteProfileTag>().RemoveRange(oldTags);
            }

            if (computed is null)
            {
                // Nothing left to summarise (e.g. every entry was deleted) - drop the stale
                // profile rather than leave outdated numbers behind.
                var stale = await _uow.Repository<TasteProfile>().GetByIdAsync(userId, cancellationToken);
                if (stale is not null)
                {
                    _uow.Repository<TasteProfile>().Remove(stale);
                }

                await _uow.SaveChangesAsync(cancellationToken);
                await _uow.CommitAsync(cancellationToken);
                return false;
            }

            await _uow.SaveChangesAsync(cancellationToken);

            var profile = await _uow.Repository<TasteProfile>().GetByIdAsync(userId, cancellationToken);
            var isNew = profile is null;
            profile ??= new TasteProfile { UserId = userId };

            profile.AverageRating = computed.AverageRating;
            profile.TotalEntries = computed.TotalEntries;
            profile.TotalReviews = computed.TotalReviews;
            profile.MostActiveMonth = computed.MostActiveMonth;
            profile.ComputedAt = computed.ComputedAt;

            if (isNew)
            {
                await _uow.Repository<TasteProfile>().AddAsync(profile, cancellationToken);
            }
            else
            {
                _uow.Repository<TasteProfile>().Update(profile);
            }

            await _uow.Repository<TasteProfileCategory>().AddRangeAsync(
                computed.Categories.Select(c => new TasteProfileCategory
                {
                    TasteProfileId = userId,
                    CategoryId = c.CategoryId,
                    EntryCount = c.EntryCount,
                    Percentage = c.Percentage
                }),
                cancellationToken);

            await _uow.Repository<TasteProfileTag>().AddRangeAsync(
                computed.TopTags.Select(t => new TasteProfileTag
                {
                    TasteProfileId = userId,
                    TagId = t.TagId,
                    UseCount = t.UseCount
                }),
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RefreshNarrativeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var repository = _uow.Repository<TasteProfile>();
        var tracked = await repository.GetByIdAsync(userId, cancellationToken);
        if (tracked is null)
        {
            return;
        }

        if (tracked.AiNarrativeGeneratedAt is { } lastGenerated
            && DateTime.UtcNow - lastGenerated < _narrativeRefreshInterval)
        {
            // Still fresh - skip the AI call rather than regenerating on every refresh cycle.
            return;
        }

        var dto = await repository.FirstOrDefaultAsync(
            tp => tp.UserId == userId, TasteProfileMappings.ToDto, cancellationToken);
        if (dto is null)
        {
            return;
        }

        var displayName = await _uow.Repository<ApplicationUser>().FirstOrDefaultAsync(
            u => u.Id == userId, u => u.DisplayName, cancellationToken);
        if (displayName is null)
        {
            return;
        }

        var narrative = await _narrativeGenerator.GenerateAsync(displayName, dto, cancellationToken);
        if (narrative is null)
        {
            return;
        }

        tracked.AiNarrative = narrative;
        tracked.AiNarrativeGeneratedAt = DateTime.UtcNow;
        repository.Update(tracked);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private const int PageSize = 100;

    private async Task<TasteProfileDto?> ComputeCoreAsync(
        string userId, CancellationToken cancellationToken)
    {
        // Repository<T>.ListAsync silently clamps pageSize to MaxPageSize (100) - a single call
        // asking for "2000" was actually only ever reading the user's first 100 entries and
        // dropping the rest (Phase 13 finding: a power user's taste profile was silently wrong).
        // A user's full entry history has no natural upper bound the way a browsing page does, so
        // this pages through everything explicitly instead of asking the shared cap to bend.
        var rows = new List<TasteEntryRow>();
        var page = 1;
        while (true)
        {
            var batch = await _uow.Entries.ListAsync(
                e => e.UserId == userId,
                e => new TasteEntryRow
                {
                    CategoryId = e.Media.CategoryId,
                    CategoryName = e.Media.Category.Name,
                    CategoryColorToken = e.Media.Category.ColorToken,
                    CategoryIcon = e.Media.Category.Icon,
                    Rating = e.Rating,
                    Review = e.Review,
                    CreatedAt = e.CreatedAt,
                    Tags = e.EntryTags
                        .Select(et => new TagDto { Id = et.TagId, Name = et.Tag.Name })
                        .ToList()
                },
                q => q.OrderBy(e => e.Id),
                page,
                PageSize,
                cancellationToken);

            rows.AddRange(batch);
            if (batch.Count < PageSize)
            {
                break;
            }

            page++;
        }

        if (rows.Count == 0)
        {
            return null;
        }

        var totalEntries = rows.Count;
        var ratings = rows.Where(r => r.Rating.HasValue).Select(r => r.Rating!.Value).ToList();

        var mostActiveMonth = rows
            .GroupBy(r => new DateTime(r.CreatedAt.Year, r.CreatedAt.Month, 1))
            .OrderByDescending(g => g.Count())
            .Select(g => (DateTime?)g.Key)
            .FirstOrDefault();

        var categories = rows
            .GroupBy(r => new { r.CategoryId, r.CategoryName, r.CategoryColorToken, r.CategoryIcon })
            .Select(g => new TasteCategorySliceDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                CategoryColorToken = g.Key.CategoryColorToken,
                Icon = g.Key.CategoryIcon,
                EntryCount = g.Count(),
                Percentage = Math.Round(100m * g.Count() / totalEntries, 1)
            })
            .OrderByDescending(c => c.EntryCount)
            .ToList();

        var topTags = rows
            .SelectMany(r => r.Tags)
            .GroupBy(t => new { t.Id, t.Name })
            .Select(g => new TasteTagDto { TagId = g.Key.Id, Name = g.Key.Name, UseCount = g.Count() })
            .OrderByDescending(t => t.UseCount)
            .Take(10)
            .ToList();

        return new TasteProfileDto
        {
            AverageRating = ratings.Count > 0
                ? Math.Round(ratings.Average(), 1, MidpointRounding.AwayFromZero)
                : null,
            TotalEntries = totalEntries,
            TotalReviews = rows.Count(r => !string.IsNullOrWhiteSpace(r.Review)),
            MostActiveMonth = mostActiveMonth,
            ComputedAt = DateTime.UtcNow,
            Categories = categories,
            TopTags = topTags
        };
    }

    /// <summary>
    /// Minimal per-entry projection for the taste computation above. Internal rather than
    /// private (with <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/> to
    /// PersonaNest.Tests) purely so unit tests can mock the repository call that produces it -
    /// nothing outside this assembly and its test project can see it.
    /// </summary>
    internal sealed class TasteEntryRow
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColorToken { get; set; } = string.Empty;
        public string? CategoryIcon { get; set; }
        public decimal? Rating { get; set; }
        public string? Review { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TagDto> Tags { get; set; } = new();
    }
}
