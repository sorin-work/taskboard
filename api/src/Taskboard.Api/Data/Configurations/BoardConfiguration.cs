using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Data.Configurations;

public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(2000);
        builder.HasOne(b => b.Workspace).WithMany().HasForeignKey(b => b.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(b => b.WorkspaceId);
    }
}

public class BoardColumnConfiguration : IEntityTypeConfiguration<BoardColumn>
{
    public void Configure(EntityTypeBuilder<BoardColumn> builder)
    {
        builder.ToTable("board_columns");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(60).IsRequired();
        builder.HasOne(c => c.Board).WithMany(b => b.Columns).HasForeignKey(c => c.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => new { c.BoardId, c.Position });
    }
}
