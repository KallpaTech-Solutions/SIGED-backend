namespace Siged.Application.DTOs.Security
{
    public class UsuarioPermisoDto
    {
        public int PermisoId { get; set; }
        public string? PermisoNombre { get; set; }
        public string? Descripcion { get; set; }
    }

    public class AsignarPermisoDirectoDto
    {
        public int UsuarioId { get; set; }
        public int PermisoId { get; set; }
    }
}