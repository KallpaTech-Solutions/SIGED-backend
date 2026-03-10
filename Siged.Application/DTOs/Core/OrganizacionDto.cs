namespace Siged.Application.DTOs.Core
{
    public class OrganizacionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Abreviatura { get; set; } = null!;
        public string Tipo { get; set; } = "Facultad";
        public string? ColorRepresentativo { get; set; }
        public string? LogoUrl { get; set; }
        public bool EstaActivo { get; set; }
    }
}