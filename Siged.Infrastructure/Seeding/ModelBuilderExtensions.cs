using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Security; // Asegúrate de que Permissions esté aquí

namespace Siged.Infrastructure.Persistence.Seeding;

public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        // 1. DEPENDENCIAS (Oficinas Administrativas)
        modelBuilder.Entity<Dependencia>().HasData(
            new Dependencia { Id = 1, Nombre = "Oficina de Tecnología de Información", Siglas = "OTI" },
            new Dependencia { Id = 2, Nombre = "Rectorado Central", Siglas = "RECT" },
            new Dependencia { Id = 3, Nombre = "Bienestar Universitario", Siglas = "DBU" },
            new Dependencia { Id = 4, Nombre = "Dirección de Admisión", Siglas = "ADM" }
        );

        // 2. ORGANIZACIONES (Facultades)
        modelBuilder.Entity<Organizacion>().HasData(
            new Organizacion { Id = 1, Nombre = "Facultad de Ingeniería en Informática y Sistemas", Abreviatura = "FIIS", Tipo = "Facultad", ColorRepresentativo = "#0284c7" },
            new Organizacion { Id = 2, Nombre = "Facultad de Zootecnia", Abreviatura = "FZ", Tipo = "Facultad", ColorRepresentativo = "#d97706" },
            new Organizacion { Id = 3, Nombre = "Facultad de Agronomía", Abreviatura = "FA", Tipo = "Facultad", ColorRepresentativo = "#16a34a" },
            new Organizacion { Id = 4, Nombre = "Facultad de Ciencias Económicas y Administrativas", Abreviatura = "FCEA", Tipo = "Facultad", ColorRepresentativo = "#9333ea" }
        );

        // 3. ROLES (Estructura Jerárquica)
        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = 1, Nombre = "SuperAdmin", Descripcion = "Control total (OTI)", Nivel = 100 },
            new Rol { Id = 2, Nombre = "Administrador", Descripcion = "Personal de oficinas centrales", Nivel = 80 },
            new Rol { Id = 3, Nombre = "Encargado", Descripcion = "Docente o administrativo de facultad", Nivel = 50 },
            new Rol { Id = 4, Nombre = "Estudiante", Descripcion = "Alumno regular UNAS", Nivel = 10 }
        );

        // 4. PERMISOS (Dinámicos)
        // Asumiendo que Permissions.All es una lista de objetos con Name, Description y Category
        var permisosData = Permissions.All.Select(p => new Permiso
        {
            IdPermiso = p.Name,
            Descripcion = p.Description,
            Categoria = p.Category
        }).ToArray();

        modelBuilder.Entity<Permiso>().HasData(permisosData);

        // 5. RELACIÓN ROLES-PERMISOS (Tabla intermedia)
        var rolPermisosSeed = Permissions.All
            .SelectMany(p => p.AssignedRoles.Select(roleId => new
            {
                RolesId = roleId,
                PermisosIdPermiso = p.Name
            }))
            .ToArray();

        modelBuilder.Entity("RolPermisos").HasData(rolPermisosSeed);

        // 6. PERSONAS (Polimorfismo TPH)
        // Importante: No repetir IDs, todos van a la misma tabla 'Personas'
        modelBuilder.Entity<Administrador>().HasData(
            new Administrador { Id = 1, DNI = "76063362", Nombres = "Benjamín", Apellidos = "Admin", DependenciaId = 1, EsPersonalInterno = true },
            new Administrador { Id = 2, DNI = "12345678", Nombres = "Juan", Apellidos = "Apoyo", DependenciaId = 2, EsPersonalInterno = true }
        );

        modelBuilder.Entity<Estudiante>().HasData(
            new Estudiante { Id = 3, DNI = "88888888", Nombres = "Pedro", Apellidos = "Alumno FIIS", CodigoEstudiante = "0020210456", EstaMatriculado = true }
        );

        modelBuilder.Entity<Encargado>().HasData(
            new Encargado { Id = 4, DNI = "44444444", Nombres = "Maria", Apellidos = "Docente Encargada", Cargo = "Director de Escuela", Oficina = "Pabellón Central" }
        );

        // 7. USUARIOS (Cuentas de Acceso)
        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Username = "admin_benjamin",
                RolId = 1,
                PersonaId = 1,
                EstaActivo = true,
                RequiereCambioPassword = false,
                PasswordHash = "$2a$12$eL7jdgl1iOF508ePe1ScyOrYqdzdDedn1yo5WuGrj1E1.ZovUk0Xe",
                FechaRegistro = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new Usuario
            {
                Id = 2,
                Username = "apoyo_juan",
                RolId = 2,
                PersonaId = 2,
                EstaActivo = true,
                RequiereCambioPassword = true,
                PasswordHash = "$2a$12$eL7jdgl1iOF508ePe1ScyOrYqdzdDedn1yo5WuGrj1E1.ZovUk0Xe",
                FechaRegistro = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new Usuario
            {
                Id = 3,
                Username = "estudiante_test",
                RolId = 4,
                PersonaId = 3,
                OrganizacionId = 1,
                EstaActivo = true,
                RequiereCambioPassword = true,
                PasswordHash = "$2a$12$eL7jdgl1iOF508ePe1ScyOrYqdzdDedn1yo5WuGrj1E1.ZovUk0Xe",
                FechaRegistro = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new Usuario
            {
                Id = 4,
                Username = "encargado_test",
                RolId = 3,
                PersonaId = 4,
                OrganizacionId = 1,
                EstaActivo = true,
                RequiereCambioPassword = true,
                PasswordHash = "$2a$12$eL7jdgl1iOF508ePe1ScyOrYqdzdDedn1yo5WuGrj1E1.ZovUk0Xe",
                FechaRegistro = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}