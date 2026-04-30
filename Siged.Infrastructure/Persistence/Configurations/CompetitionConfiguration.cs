using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.Property(c => c.MaxTeamsPerOrganization).HasDefaultValue(1);

        builder.HasOne(c => c.ChampionTeam)
            .WithMany()
            .HasForeignKey(c => c.ChampionTeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
