namespace Siged.Application.DTOs.Tournaments
{
    /// <summary>
    /// Modo inicial de competencia al configurar equipos de una sola vez.
    /// </summary>
    public enum CompetitionFormatSetupMode
    {
        /// <summary>Fase de grupos (round robin por grupo), con reparto equilibrado.</summary>
        GroupStageRoundRobin = 0,

        /// <summary>Llave de eliminación directa desde el inicio (todos los equipos).</summary>
        DirectElimination = 1
    }
}
