namespace Siged.Application.DTOs.Core
{
    public class DependenciaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Siglas { get; set; } = null!; // OTI, RECT, VRI, etc.
    }
}