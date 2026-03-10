namespace Siged.Application.DTOs.Security
{
    public class AsignarPermisosDirectosDto
    {
        public int UsuarioId { get; set; }
        public List<string> Permisos { get; set; } = new();
    }
}