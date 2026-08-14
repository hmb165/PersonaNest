using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Infrastructure.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.Property(c => c.Content).IsRequired().HasMaxLength(2000);

        builder.HasOne(c => c.Entry)
               .WithMany(e => e.Comments)
               .HasForeignKey(c => c.EntryId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
               .WithMany(u => u.Comments)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Self-reference, one level of nesting (decision D-17). Restrict, so a parent with
        // replies cannot be hard-deleted out from under them.
        builder.HasOne(c => c.ParentComment)
               .WithMany(c => c.Replies)
               .HasForeignKey(c => c.ParentCommentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DeletedBy)
               .WithMany()
               .HasForeignKey(c => c.DeletedById)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.EntryId, c.CreatedAt });

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
