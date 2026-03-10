using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence.Seeding;

namespace Siged.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        // Definimos el acceso a las entidades
        public DbSet<Persona> Personas { get; set; }
        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<Encargado> Encargados { get; set; }
        public DbSet<Administrador> Administradores { get; set; } 
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<Dependencia> Dependencias { get; set; }
        public DbSet<Organizacion> Organizaciones { get; set; }
        public DbSet<TokenInvalidado> TokensInvalidados { get; set; }
        public DbSet<AuditoriaLog> AuditoriaLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aplica las configuraciones de tablas (DNI, Nombres, etc.)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // ✅ LLAMA AL GRAN SEMBRADOR
            modelBuilder.Seed();
        }
    }
}
