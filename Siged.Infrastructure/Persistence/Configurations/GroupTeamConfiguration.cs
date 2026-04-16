using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class GroupTeamConfiguration : IEntityTypeConfiguration<GroupTeam>
    {
        public void Configure(EntityTypeBuilder<GroupTeam> builder)
        {
            // 1. Definir la Llave Primaria Compuesta
            builder.HasKey(gt => new { gt.GroupId, gt.TeamId });

            // 2. Relación con Group
            builder.HasOne(gt => gt.Group)
                .WithMany(g => g.GroupTeams)
                .HasForeignKey(gt => gt.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra el grupo, se borra la inscripción

            // 3. Relación con Team
            builder.HasOne(gt => gt.Team)
                .WithMany(t => t.GroupTeams)
                .HasForeignKey(gt => gt.TeamId)
                .OnDelete(DeleteBehavior.Restrict); // No borrar el equipo si tiene inscripciones

            // 4. Propiedades adicionales (Opcional: Nombres de columnas o restricciones)
            builder.Property(gt => gt.Points)
                .HasDefaultValue(0);

            builder.Property(gt => gt.IsQualified)
                .HasDefaultValue(false);
        }
    }
}
