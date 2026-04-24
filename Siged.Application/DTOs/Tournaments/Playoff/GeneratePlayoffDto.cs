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

        /// <summary>
        /// Si viene con al menos un cruce, se usan estos emparejamientos (equipos deben ser clasificados).
        /// Si está vacío o es null, cruces automáticos: 1° vs último, 2° vs penúltimo… en el orden
        /// Grupo A, B, C… y posiciones en tabla (típico 2 grupos: 1A vs 2B, 2A vs 1B).
        /// </summary>
        public List<PlayoffManualPairingDto>? ManualPairings { get; set; }
    }
}
