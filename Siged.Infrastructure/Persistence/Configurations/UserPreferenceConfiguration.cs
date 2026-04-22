using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Security;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
    {
        public void Configure(EntityTypeBuilder<UserPreference> builder)
        {
            builder.ToTable("UserPreferences");

            // La PK es el UserId (Relación 1:1 con Usuario)
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.WidgetsVisibles)
                .IsRequired()
                .HasMaxLength(500)
                .HasDefaultValue("usuarios,orgs,torneos,activos,recent,banner_torneos");

            builder.Property(x => x.Tema)
                .HasMaxLength(20)
                .HasDefaultValue("light");

            // Configuración de la relación 1 a 1
            builder.HasOne(x => x.Usuario)
                .WithOne() // Si tu entidad Usuario no tiene una propiedad "UserPreference", déjalo vacío
                .HasForeignKey<UserPreference>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}