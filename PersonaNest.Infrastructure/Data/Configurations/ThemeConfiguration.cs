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
            new Theme { Id = 8, Name = "Teal",            Description = "Fresh and quiet.",          PrimaryHex = "#14b8a6", PrimaryDimHex = "#0f8f80", IsDefault = false },
            new Theme { Id = 9, Name = "Indigo",          Description = "Rich and thoughtful.",      PrimaryHex = "#6366f1", PrimaryDimHex = "#4547c4", IsDefault = false },
            new Theme { Id = 10, Name = "Rose",           Description = "Soft and expressive.",      PrimaryHex = "#f43f5e", PrimaryDimHex = "#c2293f", IsDefault = false },
            new Theme { Id = 11, Name = "Lime",           Description = "Sharp and lively.",         PrimaryHex = "#84cc16", PrimaryDimHex = "#65990f", IsDefault = false },
            new Theme { Id = 12, Name = "Cyan",           Description = "Crisp and modern.",         PrimaryHex = "#06b6d4", PrimaryDimHex = "#0589a0", IsDefault = false },
            new Theme { Id = 13, Name = "Fuchsia",        Description = "Playful and vivid.",        PrimaryHex = "#d946ef", PrimaryDimHex = "#ab2cc0", IsDefault = false },
            new Theme { Id = 14, Name = "Slate",          Description = "Muted and understated.",    PrimaryHex = "#64748b", PrimaryDimHex = "#47536b", IsDefault = false },
            new Theme { Id = 15, Name = "Yellow",         Description = "Bright and cheerful.",      PrimaryHex = "#eab308", PrimaryDimHex = "#b58607", IsDefault = false },
            new Theme { Id = 16, Name = "Sky",            Description = "Light and airy.",           PrimaryHex = "#0ea5e9", PrimaryDimHex = "#0b7fb5", IsDefault = false });
    }
}
