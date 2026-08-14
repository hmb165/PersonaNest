using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.OfficialCoverUrl).HasMaxLength(400);
        builder.Property(m => m.Creator).HasMaxLength(200);
        builder.Property(m => m.Status).HasConversion<int>();
        builder.Property(m => m.AverageRating).HasPrecision(3, 1);

        // Category -> Media : Restrict. A category holding media cannot be deleted.
        builder.HasOne(m => m.Category)
               .WithMany(c => c.Media)
               .HasForeignKey(m => m.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // Submitter. No inverse navigation - ApplicationUser would otherwise carry 21 collections.
        builder.HasOne(m => m.CreatedBy)
               .WithMany()
               .HasForeignKey(m => m.CreatedById)
               .OnDelete(DeleteBehavior.Restrict);

        // Audit only: keep the media row if the removing moderator's account goes away.
        builder.HasOne(m => m.DeletedBy)
               .WithMany()
               .HasForeignKey(m => m.DeletedById)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.CategoryId, m.Status });
        builder.HasIndex(m => m.Title);

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
