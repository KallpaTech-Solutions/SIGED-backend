using System.ComponentModel.DataAnnotations;
using Siged.Application.DTOs.Tournaments.Playoff;

namespace Siged.Application.DTOs.Tournaments
{
    /// <summary>
    /// Configura la competencia: grupos + RR o eliminación directa, con sorteo opcional de equipos.
    /// </summary>
    public class SetupCompetitionFormatDto
    {
        [Required]
        public CompetitionFormatSetupMode Mode { get; set; }

        /// <summary>IDs de equipos ya inscritos en la competencia.</summary>
        [Required, MinLength(2)]
        public List<Guid> TeamIds { get; set; } = new();

        // --- Fase de grupos (Mode = GroupStageRoundRobin) ---

        /// <summary>Máximo de equipos por grupo (ej. 4 → 15 equipos → 4+4+4+3).</summary>
        [Range(2, 32)]
        public int MaxTeamsPerGroup { get; set; } = 4;

        /// <summary>Si true, mezcla los equipos antes de repartirlos en grupos.</summary>
        public bool ShuffleTeams { get; set; } = true;

        /// <summary>Cuántos clasifican de cada grupo a la siguiente fase (configurás la llave aparte).</summary>
        [Range(1, 32)]
        public int QualifiedPerGroup { get; set; } = 2;

        [MaxLength(80)]
        public string GroupPhaseName { get; set; } = "Fase de grupos";

        /// <summary>Si true, genera el fixture Berger en cada grupo (tablas de posiciones listas al cargar resultados).</summary>
        public bool AutoGenerateRoundRobinFixtures { get; set; } = true;

        // --- Eliminación directa (Mode = DirectElimination) ---

        [MaxLength(80)]
        public string KnockoutPhaseName { get; set; } = "Eliminatoria";

        /// <summary>Si true, orden aleatorio de cruces; si false, el orden de <see cref="TeamIds"/> define los pares.</summary>
        public bool KnockoutRandomSeed { get; set; } = true;

        /// <summary>
        /// Cruces manuales opcionales para eliminación directa (local/visitante por partido).
        /// Si se envía, tiene prioridad sobre el sorteo/orden automático.
        /// </summary>
        public List<PlayoffManualPairingDto>? ManualPairings { get; set; }
    }
}
