using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class GroupTeam
    {
        // Claves foráneas
        public Guid GroupId { get; set; }
        public virtual Group Group { get; set; } = null!;

        public Guid TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;

        // --- Estadísticas de la Tabla de Posiciones ---
        public int MatchesPlayed { get; set; } = 0;
        public int MatchesWon { get; set; } = 0;
        public int MatchesDrawn { get; set; } = 0;
        public int MatchesLost { get; set; } = 0;

        public int GoalsFor { get; set; } = 0;      // Goles/Puntos a favor
        public int GoalsAgainst { get; set; } = 0;  // Goles/Puntos en contra
        public int GoalsDifference => GoalsFor - GoalsAgainst;

        public int Points { get; set; } = 0;

        public bool IsQualified { get; set; } = false; // Si pasó a la siguiente ronda
    }
}
