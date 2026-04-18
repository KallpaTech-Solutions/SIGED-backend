using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Playoff
{
    public class GeneratePlayoffDto
    {
        public Guid CompetitionId { get; set; }
        public Guid SourcePhaseId { get; set; } // De qué fase vienen (ej. Fase de Grupos)
        public string NewPhaseName { get; set; } = "Cuartos de Final";
        public bool IsDoubleLeg { get; set; } = false; // ¿Ida y vuelta?
    }
}
