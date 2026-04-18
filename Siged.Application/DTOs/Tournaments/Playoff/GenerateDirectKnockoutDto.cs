using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Playoff
{
    public class GenerateDirectKnockoutDto
    {
        public Guid CompetitionId { get; set; }
        public string PhaseName { get; set; } = "Primera Ronda";
        public List<Guid> TeamIds { get; set; } = new(); // Equipos seleccionados para el sorteo
        public bool IsRandom { get; set; } = true; // ¿Mezclar aleatoriamente?
    }
}
