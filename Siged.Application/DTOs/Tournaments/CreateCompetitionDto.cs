using Siged.Domain.Entities.Core.Tournaments.Enums;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments
{
    public class CreateCompetitionDto
    {
        [Required]
        public Guid TournamentId { get; set; }

        [Required]
        public Guid DisciplineId { get; set; }

        [Required]
        public Gender Gender { get; set; } // 0: Masculino, 1: Femenino, 2: Mixto

        public string? CategoryName { get; set; } // Ej: "Cachimbos" o "Libre"

        /// <summary>Máximo de equipos por escuela. 0 permite más de un equipo sin límite.</summary>
        public int MaxTeamsPerOrganization { get; set; } = 1;
    }
}
