using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> b)
    {
        b.HasOne(t => t.CreatedByUsuario)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
