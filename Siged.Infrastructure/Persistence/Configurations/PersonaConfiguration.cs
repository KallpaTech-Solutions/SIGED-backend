using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
    {
        public void Configure(EntityTypeBuilder<Persona> builder)
        {
            builder.ToTable("Personas");

            builder.HasDiscriminator<string>("Discriminator")
                .HasValue<Estudiante>("Estudiante")
                .HasValue<Encargado>("Encargado")
                .HasValue<Administrador>("Administrador");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.DNI).IsRequired().HasMaxLength(8);
            builder.HasIndex(p => p.DNI).IsUnique();
        }
    }
}