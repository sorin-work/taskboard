using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Data.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).HasMaxLength(40).IsRequired();
        builder.Property(l => l.Color).HasMaxLength(16).IsRequired();
        builder.HasOne(l => l.Board).WithMany().HasForeignKey(l => l.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => l.BoardId);
    }
}

public class CardLabelConfiguration : IEntityTypeConfiguration<CardLabel>
{
    public void Configure(EntityTypeBuilder<CardLabel> builder)
    {
        builder.HasKey(cl => new { cl.CardId, cl.LabelId });
        builder.HasOne(cl => cl.Card).WithMany(c => c.Labels).HasForeignKey(cl => cl.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(cl => cl.Label).WithMany().HasForeignKey(cl => cl.LabelId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(cl => cl.LabelId);
    }
}
