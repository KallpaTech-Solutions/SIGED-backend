namespace Siged.Domain.Entities.Core.Tournaments.Enums
{
    /// <summary>
    /// Tipo de cronómetro / widget mostrado en la transmisión del partido.
    /// </summary>
    public enum MatchClockWidgetKind
    {
        /// <summary>Inferido desde la plantilla de la disciplina (comportamiento histórico).</summary>
        Auto = 0,

        /// <summary>No mostrar cronómetro en la vista pública ni controles de reloj en mesa.</summary>
        None = 1,

        /// <summary>Periodos con tope en PERIOD_DURATION y avance explícito de periodo.</summary>
        GenericPeriod = 2,

        /// <summary>Fútbol 11: tiempo reglamentario + tiempo añadido.</summary>
        FootballRegulation = 3,

        /// <summary>Futsal: minuto según eventos (reloj detenido).</summary>
        FutsalStopped = 4,
    }
}
