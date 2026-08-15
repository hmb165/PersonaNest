using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Message).IsRequired().HasMaxLength(300);
        builder.Property(n => n.Url).HasMaxLength(500);

        // Restrict on both - two FKs into the same table (ApplicationUser) cannot both cascade
        // without SQL Server rejecting the model, matching the Follower/FollowingUser pattern in
        // FollowConfiguration.
        builder.HasOne(n => n.Recipient)
               .WithMany()
               .HasForeignKey(n => n.RecipientUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Actor)
               .WithMany()
               .HasForeignKey(n => n.ActorUserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Covers both the unread-count query and the paged "unread first" list.
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });
        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt });
    }
}
