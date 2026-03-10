namespace Siged.Application.DTOs.Security
{
    public class UsuarioUpdateDto
    {
        public int? Id { get; set; }
        public string? Username { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Dni { get; set; }
        public int? RolId { get; set; }

        public int? DependenciaId { get; set; }
        public int? OrganizacionId { get; set; }
        public string? Cargo { get; set; }
        public string? Oficina { get; set; }
        public string? CodigoEstudiante { get; set; }
    }
}