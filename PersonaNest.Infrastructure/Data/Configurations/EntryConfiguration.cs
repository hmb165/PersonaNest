using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.Property(e => e.Rating).HasPrecision(3, 1);
        builder.Property(e => e.Review).HasMaxLength(4000);
        builder.Property(e => e.FavoriteMoment).HasMaxLength(500);
        builder.Property(e => e.PersonalCoverUrl).HasMaxLength(400);
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.Privacy).HasConversion<int>();

        builder.HasOne(e => e.User)
               .WithMany(u => u.Entries)
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Media -> Entry : Restrict. THE critical one - without it, deleting one media row
        // would cascade away every user's entries for that title (Specification v3 §4).
        builder.HasOne(e => e.Media)
               .WithMany(m => m.Entries)
               .HasForeignKey(e => e.MediaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DeletedBy)
               .WithMany()
               .HasForeignKey(e => e.DeletedById)
               .OnDelete(DeleteBehavior.SetNull);

        // One entry per user per media - no rewatch rows (decision D-11).
        builder.HasIndex(e => new { e.UserId, e.MediaId }).IsUnique();

        // Profile and dashboard feeds.
        builder.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
        // Media page community entries and the public-average aggregate.
        builder.HasIndex(e => new { e.MediaId, e.Privacy });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Entry_RatingRange",
                "[Rating] IS NULL OR ([Rating] >= 0.5 AND [Rating] <= 10.0)");
            t.HasCheckConstraint("CK_Entry_RatingHalfStep",
                "[Rating] IS NULL OR ([Rating] * 2 = FLOOR([Rating] * 2))");
        });

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
