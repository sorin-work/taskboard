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
    }
}
