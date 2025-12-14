using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activities");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Type).HasMaxLength(40).IsRequired();
        builder.Property(a => a.Data)
            .HasConversion(
                data => JsonSerializer.Serialize(data, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonSerializerOptions.Default) ?? new Dictionary<string, string>(),
                new ValueComparer<Dictionary<string, string>>(
                    (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
                    d => d.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
                    d => new Dictionary<string, string>(d)))
            .HasMaxLength(4000)
            .IsRequired();
        builder.HasOne(a => a.Board).WithMany().HasForeignKey(a => a.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Card).WithMany().HasForeignKey(a => a.CardId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.Actor).WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(a => new { a.BoardId, a.CreatedAt });
        builder.HasIndex(a => new { a.CardId, a.CreatedAt });
    }
}
