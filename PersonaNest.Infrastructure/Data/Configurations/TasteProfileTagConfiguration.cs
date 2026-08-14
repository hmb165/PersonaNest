using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class TasteProfileTagConfiguration : IEntityTypeConfiguration<TasteProfileTag>
{
    public void Configure(EntityTypeBuilder<TasteProfileTag> builder)
    {
        builder.HasOne(t => t.TasteProfile)
               .WithMany(tp => tp.Tags)
               .HasForeignKey(t => t.TasteProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Tag)
               .WithMany(tag => tag.TasteProfileTags)
               .HasForeignKey(t => t.TagId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.TasteProfileId, t.TagId }).IsUnique();
    }
}
