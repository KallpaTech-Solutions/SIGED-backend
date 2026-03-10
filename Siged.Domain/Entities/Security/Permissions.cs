using System.Collections.Generic;
using System.Linq;

namespace Siged.Domain.Entities.Security
{
    /// <summary>
    /// Estructura para definir un permiso y sus metadatos de asignación inicial.
    /// </summary>
    public record PermissionDefinition(string Name, string Description, string Category, int[] AssignedRoles);

    public static class Permissions
    {
        // --- 1. IDENTIFICADORES DE ROLES ---
        // Centralizamos los IDs para que coincidan con RolConfiguration
        public const int SuperAdminId = 1;
        public const int AdminId = 2;
        public const int EncargadoId = 3;
        public const int EstudianteId = 4;

        // --- 2. SEMILLA DE ROLES ---
        // Esta lista alimenta a RolConfiguration.cs
        public static readonly List<Rol> RolesSeed = new()
        {
            new Rol { Id = SuperAdminId, Nombre = "SuperAdmin", Descripcion = "Acceso total a la configuración del sistema SIGED." },
            new Rol { Id = AdminId, Nombre = "Admin", Descripcion = "Responsable de la OTI - Gestión de usuarios y reportes." },
            new Rol { Id = EncargadoId, Nombre = "Encargado", Descripcion = "Docente o personal encargado de una disciplina deportiva." },
            new Rol { Id = EstudianteId, Nombre = "Estudiante", Descripcion = "Alumno matriculado habilitado para participar en eventos." }
        };

        // --- 3. CATÁLOGO MAESTRO DE PERMISOS ---
        // Aquí defines TODO: Nombre, Descripción, Categoría y quién lo tiene al instalar el sistema.
        public static readonly List<PermissionDefinition> All = new()
        {
            // CATEGORÍA: SEGURIDAD
            new(SecurityUserView, "Ver lista de usuarios", "SEGURIDAD", new[] { SuperAdminId, AdminId }),
            new(SecurityUserManage, "Crear y editar usuarios", "SEGURIDAD", new[] { SuperAdminId, AdminId }),
            
            // Nota: Solo el SuperAdmin puede gestionar roles porque es una configuración crítica del sistema.
            new(SecurityRoleView, "Ver roles y permisos", "SEGURIDAD", new[] { SuperAdminId, AdminId }),
            new(SecurityRoleManage, "Gestionar roles y permisos", "SEGURIDAD", new[] { SuperAdminId,AdminId }),

            // CATEGORÍA: CORE (Organización)
            new(CoreOrgView, "Ver facultades y sedes", "CORE", new[] { SuperAdminId, AdminId, EncargadoId, EstudianteId }),
            new(CoreOrgManage, "Administrar facultades", "CORE", new[] { SuperAdminId }),

            // CATEGORÍA: COMPETENCIAS
            new(CompTournView, "Ver torneos y resultados", "COMPETENCIAS", new[] { SuperAdminId, AdminId, EncargadoId, EstudianteId }),
            new(CompTournManage, "Gestionar fechas y torneos", "COMPETENCIAS", new[] { SuperAdminId, EncargadoId }),
        };

        // --- 4. CONSTANTES DE STRINGS (Para usar en [Authorize(Policy = ...)]) ---
        // Mantener estas constantes te ayuda a evitar errores de dedo en los Controladores.
        public const string SecurityUserView = "security.user.view";
        public const string SecurityUserManage = "security.user.manage";
        
        public const string SecurityRoleManage = "security.role.manage";
        public const string SecurityRoleView = "security.role.view";

        public const string CoreOrgView = "core.org.view";
        public const string CoreOrgManage = "core.org.manage";

        public const string CompTournView = "comp.tourn.view";
        public const string CompTournManage = "comp.tourn.manage";

        // --- 5. MÉTODOS AUXILIARES ---
        public static List<string> GetAllNames() => All.Select(p => p.Name).ToList();
    }
}