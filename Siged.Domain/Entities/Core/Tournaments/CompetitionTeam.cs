using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class CompetitionTeam
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CompetitionId { get; set; }
        public virtual Competition Competition { get; set; } = null!;

        public Guid TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;

        public DateTime FechaInscripcion { get; set; } = DateTime.UtcNow;

        // Para el motor de tablas de posiciones
        public int Puntos { get; set; } = 0;
        public int PartidosJugados { get; set; } = 0;
        public bool EstaDescalificado { get; set; } = false;
    }
}
