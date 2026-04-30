using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class MatchLineupConfiguration : IEntityTypeConfiguration<MatchLineup>
{
    public void Configure(EntityTypeBuilder<MatchLineup> builder)
    {
        builder.HasIndex(l => new { l.MatchId, l.TeamId }).IsUnique();
        builder.Property(l => l.Status).HasConversion<string>();
        builder.Property(l => l.Observation).HasMaxLength(500);

        builder.HasOne(l => l.Match)
            .WithMany(m => m.Lineups)
            .HasForeignKey(l => l.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Team)
            .WithMany()
            .HasForeignKey(l => l.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
