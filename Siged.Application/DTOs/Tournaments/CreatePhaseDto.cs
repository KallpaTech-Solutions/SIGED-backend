using Siged.Domain.Entities.Core.Tournaments.Enums;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments
{
    public class CreatePhaseDto
    {
        [Required]
        public Guid CompetitionId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty; // Ej: "Fase de Grupos"

        [Required]
        public PhaseType Type { get; set; } // RoundRobin o EliminacionDirecta

        public int Sequence { get; set; } // 1, 2, 3...
    }
}
