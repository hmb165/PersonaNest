using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasOne(f => f.Follower)
               .WithMany(u => u.Following)
               .HasForeignKey(f => f.FollowerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.FollowingUser)
               .WithMany(u => u.Followers)
               .HasForeignKey(f => f.FollowingId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();
        builder.HasIndex(f => f.FollowerId);
        builder.HasIndex(f => f.FollowingId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Follow_NotSelf", "[FollowerId] <> [FollowingId]"));
    }
}
