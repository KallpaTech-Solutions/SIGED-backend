using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class CompetitionTeamConfiguration : IEntityTypeConfiguration<CompetitionTeam>
{
    public void Configure(EntityTypeBuilder<CompetitionTeam> builder)
    {
        builder.HasIndex(ct => new { ct.CompetitionId, ct.TeamId }).IsUnique();
        builder.Property(ct => ct.RosterLocked).HasDefaultValue(false);
    }
}
