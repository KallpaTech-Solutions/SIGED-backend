using Siged.Domain.Entities.Core.Tournaments.Enums;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Match
{
    public class MatchEventDto
    {
        [Required]
        public int Minute { get; set; }

        [Required]
        public MatchEventType Type { get; set; } // Gol, Triple, Punto de Set, etc.

        [Required]
        public Guid TeamId { get; set; }

        public Guid? PlayerId { get; set; }

        public string? Note { get; set; }

        // --- Campos para multideporte ---

        [Range(0, 100)]
        public int Value { get; set; } = 0; // 0 ej. tiro libre sin conversión; 1 gol; 2–3 básquet

        [Required]
        public int Period { get; set; } // 1 para 1T, 2 para 2T, etc.
    }
}
