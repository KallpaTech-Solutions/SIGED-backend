using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class MatchConfiguration : IEntityTypeConfiguration<Match>
    {
        public void Configure(EntityTypeBuilder<Match> builder)
        {
            builder.ToTable("Matches");

            // ✅ Configuración para evitar conflictos de "Cascada" en las llaves foráneas
            builder.HasOne(m => m.LocalTeam)
                   .WithMany()
                   .HasForeignKey(m => m.LocalTeamId)
                   .OnDelete(DeleteBehavior.Restrict); // No borrar equipo si hay partido

            builder.HasOne(m => m.VisitorTeam)
                   .WithMany()
                   .HasForeignKey(m => m.VisitorTeamId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(m => m.Status).HasConversion<string>();
        }
    }
}