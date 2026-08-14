using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Privacy).HasConversion<int>();

        builder.HasOne(c => c.User)
               .WithMany(u => u.Collections)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
