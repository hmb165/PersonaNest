using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(60);
        builder.Property(c => c.Description).HasMaxLength(200);
        builder.Property(c => c.Icon).HasMaxLength(8);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(64);
        builder.Property(c => c.ColorToken).IsRequired().HasMaxLength(32);

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Slug).IsUnique();

        // The seven categories rendered as home-page pills and search filters (decision D-18).
        builder.HasData(
            new Category { Id = 1, Name = "Games",    Slug = "games",    Icon = "\U0001F3AE", ColorToken = "success", Description = "Video games across all platforms" },
            new Category { Id = 2, Name = "Movies",   Slug = "movies",   Icon = "\U0001F3AC", ColorToken = "primary", Description = "Feature films and documentaries" },
            new Category { Id = 3, Name = "TV Shows", Slug = "tv-shows", Icon = "\U0001F4FA", ColorToken = "primary", Description = "Series and limited series" },
            new Category { Id = 4, Name = "Anime",    Slug = "anime",    Icon = "\u2728",     ColorToken = "accent",  Description = "Animated series and films" },
            new Category { Id = 5, Name = "Manga",    Slug = "manga",    Icon = "\U0001F4D6", ColorToken = "accent",  Description = "Manga and graphic novels" },
            new Category { Id = 6, Name = "Books",    Slug = "books",    Icon = "\U0001F4DA", ColorToken = "warning", Description = "Fiction and non-fiction" },
            new Category { Id = 7, Name = "Music",    Slug = "music",    Icon = "\U0001F3B5", ColorToken = "info",    Description = "Albums, EPs, and singles" });
    }
}
