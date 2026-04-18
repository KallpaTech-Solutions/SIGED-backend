using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Playoff
{
    public class PromoteWinnersDto
    {
        public Guid CompetitionId { get; set; }
        public Guid CurrentPhaseId { get; set; } // De dónde vienen
        [Required(ErrorMessage = "El nombre de la siguiente fase es obligatorio.")]
        public required string NextPhaseName { get; set; } // "Semifinal", "Final", etc.
    }
}
