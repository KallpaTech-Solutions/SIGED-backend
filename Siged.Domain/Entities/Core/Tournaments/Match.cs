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
        public Guid LocalTeamId { get; set; }
        public virtual Team LocalTeam { get; set; } = null!;
        public Guid VisitorTeamId { get; set; }
        public virtual Team VisitorTeam { get; set; } = null!;

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

        public virtual ICollection<MatchEvent> Events { get; set; } = new List<MatchEvent>();
    }
}
