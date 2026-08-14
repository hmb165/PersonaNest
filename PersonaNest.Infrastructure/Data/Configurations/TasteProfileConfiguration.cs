using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class TasteProfileConfiguration : IEntityTypeConfiguration<TasteProfile>
{
    public void Configure(EntityTypeBuilder<TasteProfile> builder)
    {
        // Shared primary key with ApplicationUser (one-to-one).
        builder.HasKey(tp => tp.UserId);

        builder.Property(tp => tp.AverageRating).HasPrecision(3, 1);

        builder.HasOne(tp => tp.User)
               .WithOne(u => u.TasteProfile)
               .HasForeignKey<TasteProfile>(tp => tp.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
