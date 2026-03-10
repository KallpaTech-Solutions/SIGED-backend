namespace Siged.Application.DTOs.Security
{
    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!; // ✅ Útil para búsquedas rápidas
        public string NombreCompleto { get; set; } = null!;
        public string Dni { get; set; } = null!;
        public string Rol { get; set; } = null!;
        public bool EstaActivo { get; set; }

        // 🏛️ IDENTIFICACIÓN INSTITUCIONAL
        // Reemplazamos 'NombreOrganizacion' por algo más genérico y sus siglas
        public string Entidad { get; set; } = null!; // Mostrará "OTI", "FIIS", "RECT", etc.
        public string? NombreInstitucion { get; set; } // Nombre largo (ej. "Rectorado Central")

        // 💡 Mantener el ID por si el Front necesita hacer un link a la Facultad/Oficina
        public int? OrganizacionId { get; set; }
        public int? DependenciaId { get; set; }
    }
}