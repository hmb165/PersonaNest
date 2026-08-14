using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class EntryLikeConfiguration : IEntityTypeConfiguration<EntryLike>
{
    public void Configure(EntityTypeBuilder<EntryLike> builder)
    {
        builder.HasOne(l => l.Entry)
               .WithMany(e => e.Likes)
               .HasForeignKey(l => l.EntryId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // One like per user per entry.
        builder.HasIndex(l => new { l.EntryId, l.UserId }).IsUnique();
    }
}
