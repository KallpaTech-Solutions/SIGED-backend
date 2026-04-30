using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class MatchLineupPlayerConfiguration : IEntityTypeConfiguration<MatchLineupPlayer>
{
    public void Configure(EntityTypeBuilder<MatchLineupPlayer> builder)
    {
        builder.HasIndex(p => new { p.MatchLineupId, p.PlayerId }).IsUnique();
        builder.Property(p => p.Role).HasConversion<string>();
        builder.Property(p => p.Position).HasConversion<int>();
        builder.Property(p => p.Observation).HasMaxLength(500);

        builder.HasOne(p => p.MatchLineup)
            .WithMany(l => l.Players)
            .HasForeignKey(p => p.MatchLineupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Player)
            .WithMany(player => player.MatchLineupPlayers)
            .HasForeignKey(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
