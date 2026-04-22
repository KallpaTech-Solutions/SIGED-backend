namespace Siged.Domain.Entities.Core.Tournaments.Enums
{
    /// <summary>
    /// Etapa del ciclo de vida del torneo (mensaje al usuario y reglas de negocio).
    /// Valores fijos para persistencia en BD (entero).
    /// </summary>
    public enum TournamentStatus
    {
        Borrador = 0,
        /// <summary>Se aceptan inscripciones de equipos.</summary>
        InscripcionesAbiertas = 1,
        Activo = 2,
        Finalizado = 3,
        /// <summary>
        /// Inscripciones cerradas; se arma el fixture y el calendario. Aún no hay competencia “en juego” oficial.
        /// Transición típica: InscripcionesAbiertas → Programado → Activo.
        /// </summary>
        Programado = 4,
    }
}
