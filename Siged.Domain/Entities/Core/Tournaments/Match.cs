using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Match
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // 🔗 Relación con la Jornada (Faltaba esto)
        public Guid JournalId { get; set; }
        public virtual Journal Journal { get; set; } = null!;
        public Guid? VenueId { get; set; } // Sede
        public virtual Venue? Venue { get; set; }

        // Equipos (Usaremos tus nombres: Local y Visitor)
        public Guid? LocalTeamId { get; set; }
        public virtual Team? LocalTeam { get; set; } = null!;
        public Guid? VisitorTeamId { get; set; }
        public virtual Team? VisitorTeam { get; set; } = null!;

        public Guid PhaseId { get; set; }
        public virtual Phase Phase { get; set; } = null!;
        public Guid DisciplineId { get; set; }
        public virtual Discipline Discipline { get; set; } = null!;
        public Guid? GroupId { get; set; }

        public DateTime ScheduledAt { get; set; }
        public MatchStatus Status { get; set; } = MatchStatus.Programado;

        // Resultados calculados (se pueden llenar al finalizar)
        public int LocalScore { get; set; }
        public int VisitorScore { get; set; }
        public bool IsActive { get; set; } = true;
        public int? LocalPenaltyScore { get; set; }  // Goles en tanda de penales
        public int? VisitorPenaltyScore { get; set; }
        public string? Note { get; set; } // Para anotaciones especiales (ej. "Pasa libre", "W.O.", etc.)
        public Guid? WinnerId { get; set; } // El ID del equipo que avanza (crucial para llaves)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Segundos de juego ya acumulados del periodo actual (p. ej. antes de pausar transmisión).
        /// Tiempo mostrado = ClockAccumulatedSeconds + (ahora - ClockPeriodAnchorUtc) cuando hay ancla.
        /// </summary>
        public int ClockAccumulatedSeconds { get; set; }

        /// <summary>
        /// Inicio del tramo actual del cronómetro en UTC (null si no está corriendo: pausa de transmisión o descanso).
        /// </summary>
        public DateTime? ClockPeriodAnchorUtc { get; set; }

        /// <summary>
        /// Qué widget de cronómetro usa esta transmisión (<see cref="MatchClockWidgetKind.None"/> = oculto).
        /// </summary>
        public MatchClockWidgetKind ClockWidgetKind { get; set; } = MatchClockWidgetKind.Auto;

        /// <summary>
        /// Ventana temporal (UTC) para permitir enviar/corregir planilla del local aun con listas cerradas/en vivo.
        /// </summary>
        public DateTime? LocalLineupOpenUntilUtc { get; set; }

        /// <summary>
        /// Ventana temporal (UTC) para permitir enviar/corregir planilla del visitante aun con listas cerradas/en vivo.
        /// </summary>
        public DateTime? VisitorLineupOpenUntilUtc { get; set; }

        public virtual ICollection<MatchEvent> Events { get; set; } = new List<MatchEvent>();
        public virtual ICollection<MatchLineup> Lineups { get; set; } = new List<MatchLineup>();
    }
}
