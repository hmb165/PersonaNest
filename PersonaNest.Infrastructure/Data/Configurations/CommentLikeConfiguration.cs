using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
{
    public void Configure(EntityTypeBuilder<CommentLike> builder)
    {
        builder.HasOne(l => l.Comment)
               .WithMany(c => c.Likes)
               .HasForeignKey(l => l.CommentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // One like per user per comment.
        builder.HasIndex(l => new { l.CommentId, l.UserId }).IsUnique();
    }
}
