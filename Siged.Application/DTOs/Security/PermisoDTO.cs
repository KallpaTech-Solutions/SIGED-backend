namespace Siged.Application.DTOs.Security
{
    public class PermisoDTO
    {
        public string IdPermiso { get; set; } = null!;
        public string Categoria { get; set; } = null!;
        public string? Descripcion { get; set; }
    }
}