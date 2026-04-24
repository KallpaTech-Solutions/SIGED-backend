using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> b)
    {
        b.HasIndex(p => new { p.TeamId, p.Dni }).IsUnique();
    }
}
