namespace Siged.Application.DTOs.Security
{
    public class UserCreateDto
    {
        // --- Datos de Persona (Comunes) ---
        public required string DNI { get; set; }
        public required string Nombres { get; set; }
        public required string Apellidos { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }

        // --- Datos de Cuenta de Usuario ---
        public required string Username { get; set; }
        public required string Password { get; set; }
        public int RolId { get; set; } // 1: SuperAdmin, 2: Admin, 3: Encargado, 4: Estudiante

        // --- ✅ Datos Específicos de Administrador (NUEVO) ---
        public int? DependenciaId { get; set; } // FK a la tabla Dependencias
        public bool EsPersonalInterno { get; set; } = true;

        // --- Datos Específicos de Estudiante ---
        public string? CodigoEstudiante { get; set; }
        public int? OrganizacionId { get; set; }

        // --- Datos Específicos de Encargado ---
        public string? Cargo { get; set; }
        public string? Oficina { get; set; }
    }
}