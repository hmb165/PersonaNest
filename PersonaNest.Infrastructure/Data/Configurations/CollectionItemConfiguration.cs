using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class CollectionItemConfiguration : IEntityTypeConfiguration<CollectionItem>
{
    public void Configure(EntityTypeBuilder<CollectionItem> builder)
    {
        builder.HasKey(ci => new { ci.CollectionId, ci.MediaId });

        builder.HasOne(ci => ci.Collection)
               .WithMany(c => c.Items)
               .HasForeignKey(ci => ci.CollectionId)
               .OnDelete(DeleteBehavior.Cascade);

        // Restrict: removing a media row must not silently gut users' collections.
        builder.HasOne(ci => ci.Media)
               .WithMany(m => m.CollectionItems)
               .HasForeignKey(ci => ci.MediaId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
