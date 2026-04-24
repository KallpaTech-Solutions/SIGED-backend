using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations;

public class TeamGestorConfiguration : IEntityTypeConfiguration<TeamGestor>
{
    public void Configure(EntityTypeBuilder<TeamGestor> b)
    {
        b.ToTable("TeamGestores");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.TeamId, x.UsuarioId }).IsUnique();
        b.HasOne(x => x.Team)
            .WithMany(t => t.Gestores)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Usuario)
            .WithMany(u => u.EquiposGestionados)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
