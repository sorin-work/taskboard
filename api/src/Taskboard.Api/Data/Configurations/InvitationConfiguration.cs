using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Email).HasMaxLength(254).IsRequired();
        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(16);
        builder.Property(i => i.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => new { i.WorkspaceId, i.Email });
        builder.HasOne(i => i.Workspace).WithMany().HasForeignKey(i => i.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.InvitedBy).WithMany().HasForeignKey(i => i.InvitedById).OnDelete(DeleteBehavior.Cascade);
    }
}
