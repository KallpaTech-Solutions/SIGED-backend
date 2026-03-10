namespace Siged.Application.DTOs.Security
{
    public class RoleSaveDto
    {
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public int Nivel { get; set; } // Jerarquía de 1 a 100
        public List<string> Permisos { get; set; } = new(); // Lista de IDs de permisos (strings)
    }
}
