using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class MatchConfiguration : IEntityTypeConfiguration<Match>
    {
        public void Configure(EntityTypeBuilder<Match> builder)
        {
            builder.ToTable("Matches");

            builder.Property(m => m.ClockWidgetKind)
                .HasConversion<int>()
                .HasDefaultValue(MatchClockWidgetKind.Auto);

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