using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class PlayerSanctionConfiguration : IEntityTypeConfiguration<PlayerSanction>
{
    public void Configure(EntityTypeBuilder<PlayerSanction> builder)
    {
        builder.Property(s => s.Type).HasConversion<string>();
        builder.Property(s => s.Reason).HasMaxLength(300);
        builder.Property(s => s.Observation).HasMaxLength(1000);
        builder.HasIndex(s => new { s.PlayerId, s.CompetitionId, s.IsActive });

        builder.HasOne(s => s.Player)
            .WithMany(p => p.Sanctions)
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Competition)
            .WithMany()
            .HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Team)
            .WithMany()
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Match)
            .WithMany()
            .HasForeignKey(s => s.MatchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.MatchEvent)
            .WithMany()
            .HasForeignKey(s => s.MatchEventId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
