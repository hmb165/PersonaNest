using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class ThemeConfiguration : IEntityTypeConfiguration<Theme>
{
    public void Configure(EntityTypeBuilder<Theme> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(60);
        builder.Property(t => t.Description).HasMaxLength(200);
        builder.Property(t => t.PrimaryHex).IsRequired().HasMaxLength(7).IsFixedLength();
        builder.Property(t => t.PrimaryDimHex).IsRequired().HasMaxLength(7).IsFixedLength();

        builder.HasIndex(t => t.Name).IsUnique();

        // The design system's eight accent swatches (decision D-3).
        builder.HasData(
            new Theme { Id = 1, Name = "Electric Violet", Description = "The PersonaNest default.",   PrimaryHex = "#7c5cfc", PrimaryDimHex = "#5b3fe8", IsDefault = true  },
            new Theme { Id = 2, Name = "Hot Pink",        Description = "Bold and warm.",            PrimaryHex = "#ec4899", PrimaryDimHex = "#be2f77", IsDefault = false },
            new Theme { Id = 3, Name = "Sunset Orange",   Description = "Bright and energetic.",     PrimaryHex = "#f97316", PrimaryDimHex = "#c25a0f", IsDefault = false },
            new Theme { Id = 4, Name = "Emerald",         Description = "Calm and natural.",         PrimaryHex = "#10b981", PrimaryDimHex = "#0b8f65", IsDefault = false },
            new Theme { Id = 5, Name = "Ocean Blue",      Description = "Cool and steady.",          PrimaryHex = "#3b82f6", PrimaryDimHex = "#2c62c4", IsDefault = false },
            new Theme { Id = 6, Name = "Amber",           Description = "Golden and warm.",          PrimaryHex = "#f59e0b", PrimaryDimHex = "#c37e09", IsDefault = false },
            new Theme { Id = 7, Name = "Crimson",         Description = "Deep and dramatic.",        PrimaryHex = "#ef4444", PrimaryDimHex = "#c02f2f", IsDefault = false },
            new Theme { Id = 8, Name = "Teal",            Description = "Fresh and quiet.",          PrimaryHex = "#14b8a6", PrimaryDimHex = "#0f8f80", IsDefault = false });
    }
}
