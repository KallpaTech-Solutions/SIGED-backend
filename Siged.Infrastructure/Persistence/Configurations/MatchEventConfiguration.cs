using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class MatchEventConfiguration : IEntityTypeConfiguration<MatchEvent>
{
    public void Configure(EntityTypeBuilder<MatchEvent> builder)
    {
        builder.HasOne(e => e.RelatedPlayer)
            .WithMany()
            .HasForeignKey(e => e.RelatedPlayerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
