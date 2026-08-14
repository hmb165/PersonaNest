using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class TasteProfileCategoryConfiguration : IEntityTypeConfiguration<TasteProfileCategory>
{
    public void Configure(EntityTypeBuilder<TasteProfileCategory> builder)
    {
        builder.Property(c => c.Percentage).HasPrecision(5, 2);

        builder.HasOne(c => c.TasteProfile)
               .WithMany(tp => tp.Categories)
               .HasForeignKey(c => c.TasteProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Category)
               .WithMany(cat => cat.TasteProfileCategories)
               .HasForeignKey(c => c.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TasteProfileId, c.CategoryId }).IsUnique();
    }
}
