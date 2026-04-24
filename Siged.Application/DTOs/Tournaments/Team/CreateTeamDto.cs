using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Team
{
    public class CreateTeamDto
    {
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        /// <summary>Obligatorio para administradores de torneo. Los delegados lo ignoran: el API usa la organización del usuario.</summary>
        public int OrganizacionId { get; set; }

        [MaxLength(5, ErrorMessage = "Las iniciales no pueden tener más de 5 caracteres.")]
        public string? Initials { get; set; } // Ej: "INF", "AGR"

        public string? RepresentativeName { get; set; } // Nombre del delegado

        /// <summary>
        /// Obligatorio si quien crea es SuperAdmin: ID del usuario que será delegado principal del equipo
        /// (debe pertenecer a <see cref="OrganizacionId"/>). Los delegados de escuela ignoran este campo.
        /// </summary>
        public int? PrincipalUsuarioId { get; set; }

        // El archivo físico que viene desde Swagger o React
        public IFormFile? LogoFile { get; set; }
    }
}
