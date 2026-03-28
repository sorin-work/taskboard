using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Data.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(10000);
        builder.HasOne(c => c.Board).WithMany().HasForeignKey(c => c.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Column).WithMany().HasForeignKey(c => c.ColumnId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => new { c.ColumnId, c.Position });

        // Due date filters on a board and the my tasks list.
        builder.HasIndex(c => new { c.BoardId, c.DueDate });
    }
}

public class CardAssigneeConfiguration : IEntityTypeConfiguration<CardAssignee>
{
    public void Configure(EntityTypeBuilder<CardAssignee> builder)
    {
        builder.HasKey(a => new { a.CardId, a.UserId });
        builder.HasOne(a => a.Card).WithMany(c => c.Assignees).HasForeignKey(a => a.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(a => a.UserId);
    }
}
