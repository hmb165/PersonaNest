using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(60);
        builder.Property(u => u.Bio).HasMaxLength(500);
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(400);
        builder.Property(u => u.BannerUrl).HasMaxLength(400);
        builder.Property(u => u.AccentColor).HasMaxLength(7).IsFixedLength();
        builder.Property(u => u.BanReason).HasMaxLength(300);
        builder.Property(u => u.DefaultEntryPrivacy).HasConversion<int>();

        // Theme -> ApplicationUser : Restrict. A theme in use cannot be deleted.
        builder.HasOne(u => u.Theme)
               .WithMany(t => t.Users)
               .HasForeignKey(u => u.ThemeId)
               .OnDelete(DeleteBehavior.Restrict);

        // Supports the Users tab of Search (design system).
        builder.HasIndex(u => u.DisplayName).HasDatabaseName("IX_AspNetUsers_DisplayName");

        // NOTE: ApplicationUser deliberately has NO global query filter.
        // Account deletion anonymises in place (decision D-17) and content stays attributed to
        // the row, so every required Entry/Comment -> User navigation must still resolve.
        // See PersonaNestDbContext for the full reasoning.
    }
}
