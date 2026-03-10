using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Security;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");

            // --- Relaciones existentes ---
            builder.HasOne(u => u.Creador)
                   .WithMany()
                   .HasForeignKey(u => u.CreadoPorUsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.Persona)
                   .WithOne()
                   .HasForeignKey<Usuario>(u => u.PersonaId);

            // ✅ AQUÍ AGREGAMOS LA RELACIÓN CON EL ROL
            builder.HasOne(u => u.Rol)
                   .WithMany() // Un Rol puede estar en muchos Usuarios
                   .HasForeignKey(u => u.RolId)
                   .OnDelete(DeleteBehavior.Restrict); // Evita borrar roles que tengan usuarios

            // --- Relación de permisos especiales ---
            builder.HasMany(u => u.PermisosEspeciales)
                   .WithMany(p => p.Usuarios)
                   .UsingEntity(j => j.ToTable("UsuariosPermisos"));
        }
    }
}