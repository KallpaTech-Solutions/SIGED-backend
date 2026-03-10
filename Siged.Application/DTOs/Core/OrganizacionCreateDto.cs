namespace Siged.Application.DTOs.Core
{
    public class OrganizacionCreateDto
    {
        public required string Nombre { get; set; }
        public required string Abreviatura { get; set; }
        public string Tipo { get; set; } = "Facultad";
        public string? Descripcion { get; set; }
        public string? Lema { get; set; }
        public string? ColorRepresentativo { get; set; }
        public string? LogoUrl { get; set; }
        public string? PortadaUrl { get; set; }
        public DateTime? FechaCreacion { get; set; } // Agregado
        public bool EstaActivo { get; set; } = true;  // Agregado
    }
}