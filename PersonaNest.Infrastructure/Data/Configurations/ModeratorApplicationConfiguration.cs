using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class ModeratorApplicationConfiguration : IEntityTypeConfiguration<ModeratorApplication>
{
    public void Configure(EntityTypeBuilder<ModeratorApplication> builder)
    {
        builder.Property(a => a.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(a => a.RelevantExperience).HasMaxLength(1000);
        builder.Property(a => a.AdminNotes).HasMaxLength(2000);
        builder.Property(a => a.Status).HasConversion<int>();

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Admins review applications. SetNull keeps the application if the admin account goes.
        builder.HasOne(a => a.ReviewedByAdmin)
               .WithMany()
               .HasForeignKey(a => a.ReviewedByAdminId)
               .OnDelete(DeleteBehavior.SetNull);

        // Filtered unique index: at most one Pending application per user (Status 0 = Pending).
        builder.HasIndex(a => a.UserId)
               .IsUnique()
               .HasFilter("[Status] = 0")
               .HasDatabaseName("UX_ModeratorApplication_User_Pending");
    }
}
