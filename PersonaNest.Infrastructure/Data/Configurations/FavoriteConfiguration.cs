using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasOne(f => f.User)
               .WithMany(u => u.Favorites)
               .HasForeignKey(f => f.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Media)
               .WithMany(m => m.Favorites)
               .HasForeignKey(f => f.MediaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.UserId, f.MediaId }).IsUnique();
    }
}
