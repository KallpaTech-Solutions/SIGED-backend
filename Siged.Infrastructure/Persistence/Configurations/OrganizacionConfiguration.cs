using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class OrganizacionConfiguration : IEntityTypeConfiguration<Organizacion>
    {
        public void Configure(EntityTypeBuilder<Organizacion> builder)
        {
            // 1. Nombre de la tabla
            builder.ToTable("Organizaciones");

            // 2. Llave Primaria
            builder.HasKey(o => o.Id);

            // 3. Propiedades y límites de tamaño (Muy importante para optimizar la BD)
            builder.Property(o => o.Nombre)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(o => o.Abreviatura)
                   .IsRequired()
                   .HasMaxLength(15); // Ej: "FIIS"

            builder.Property(o => o.Tipo)
                   .IsRequired()
                   .HasMaxLength(50)
                   .HasDefaultValue("Facultad");

            builder.Property(o => o.ColorRepresentativo)
                   .HasMaxLength(7); // Tamaño exacto para un código HEX como "#004080"

            builder.Property(o => o.Lema)
                   .HasMaxLength(250);

            builder.Property(o => o.LogoUrl)
                   .HasMaxLength(500);

            builder.Property(o => o.PortadaUrl)
                   .HasMaxLength(500);

            // 4. Índices Únicos (Reglas de negocio)
            // Impide que se creen dos facultades con el mismo nombre o abreviatura
            builder.HasIndex(o => o.Nombre).IsUnique();
            builder.HasIndex(o => o.Abreviatura).IsUnique();

            // 5. Configurar la Relación (1 Organización -> Muchos Usuarios)
            builder.HasMany(o => o.Usuarios)
                   .WithOne(u => u.Organizacion) // Asegúrate de haber puesto public Organizacion? Organizacion {get;set;} en tu clase Usuario
                   .HasForeignKey(u => u.OrganizacionId)
                   .OnDelete(DeleteBehavior.Restrict); // Restrict evita que si borras una facultad, se borren todos sus estudiantes
            
        }
    }
}