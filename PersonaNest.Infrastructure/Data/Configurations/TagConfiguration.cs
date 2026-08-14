using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.NormalizedName).IsRequired().HasMaxLength(50);

        // Prevents "SciFi", "sci-fi " and "Sci-Fi" becoming three tags.
        builder.HasIndex(t => t.NormalizedName).IsUnique();

        builder.HasData(
            new Tag { Id =  1, Name = "Masterpiece",        NormalizedName = "MASTERPIECE" },
            new Tag { Id =  2, Name = "Emotional",          NormalizedName = "EMOTIONAL" },
            new Tag { Id =  3, Name = "Comfort Media",      NormalizedName = "COMFORT MEDIA" },
            new Tag { Id =  4, Name = "Childhood Favorite", NormalizedName = "CHILDHOOD FAVORITE" },
            new Tag { Id =  5, Name = "Hidden Gem",         NormalizedName = "HIDDEN GEM" },
            new Tag { Id =  6, Name = "Story Rich",         NormalizedName = "STORY RICH" },
            new Tag { Id =  7, Name = "RPG",                NormalizedName = "RPG" },
            new Tag { Id =  8, Name = "Sci-Fi",             NormalizedName = "SCI-FI" },
            new Tag { Id =  9, Name = "Fantasy",            NormalizedName = "FANTASY" },
            new Tag { Id = 10, Name = "Rewatch",            NormalizedName = "REWATCH" },
            new Tag { Id = 11, Name = "Underrated",         NormalizedName = "UNDERRATED" },
            new Tag { Id = 12, Name = "OST Standout",       NormalizedName = "OST STANDOUT" });
    }
}
