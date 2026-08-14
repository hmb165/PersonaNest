using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class MediaReportConfiguration : IEntityTypeConfiguration<MediaReport>
{
    public void Configure(EntityTypeBuilder<MediaReport> builder)
    {
        builder.Property(r => r.Reason).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.ResolutionNotes).HasMaxLength(2000);

        builder.HasOne(r => r.Reporter)
               .WithMany()
               .HasForeignKey(r => r.ReporterId)
               .OnDelete(DeleteBehavior.Restrict);

        // Real foreign key to the target - the reason these stayed three tables (decision D-4).
        builder.HasOne(r => r.Media)
               .WithMany(t => t.Reports)
               .HasForeignKey(r => r.MediaId)
               .OnDelete(DeleteBehavior.Restrict);

        // SetNull so the report and its ResolutionNotes survive as an audit record.
        builder.HasOne(r => r.ReviewedBy)
               .WithMany()
               .HasForeignKey(r => r.ReviewedById)
               .OnDelete(DeleteBehavior.SetNull);

        // Drives the moderation queue.
        builder.HasIndex(r => new { r.Status, r.CreatedAt }).IsDescending(false, true);
    }
}
