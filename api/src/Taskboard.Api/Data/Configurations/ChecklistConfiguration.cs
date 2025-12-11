using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Data.Configurations;

public class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(100).IsRequired();
        builder.HasOne(c => c.Card).WithMany(c => c.Checklists).HasForeignKey(c => c.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.CardId);
    }
}

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Text).HasMaxLength(300).IsRequired();
        builder.HasOne(i => i.Checklist).WithMany(c => c.Items).HasForeignKey(i => i.ChecklistId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(i => i.ChecklistId);
    }
}
