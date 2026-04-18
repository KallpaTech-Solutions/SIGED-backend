using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Team
{
    public class CreateTeamDto
    {
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        // 🚀 Vínculo con la Escuela
        [Required(ErrorMessage = "La organización (Escuela) es obligatoria.")]
        public int OrganizacionId { get; set; }

        [MaxLength(5, ErrorMessage = "Las iniciales no pueden tener más de 5 caracteres.")]
        public string? Initials { get; set; } // Ej: "INF", "AGR"

        public string? RepresentativeName { get; set; } // Nombre del delegado

        // El archivo físico que viene desde Swagger o React
        public IFormFile? LogoFile { get; set; }
    }
}
