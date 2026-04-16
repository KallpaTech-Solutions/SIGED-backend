using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core.Tournaments.Enums
{
    public enum MatchEventType
    {
        Puntaje = 1,      // Goles, Canastas, Puntos de set
        Goal = 2,
        TarjetaAmarilla = 3,
        TarjetaRoja = 4,
        Sustitucion = 5,
        Falta = 6,
        InicioPeriodo = 7, // Para marcar cuando empieza el 2T o el Set 3
        FinPeriodo = 8
    }
}
