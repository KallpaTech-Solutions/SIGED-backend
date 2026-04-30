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
        PenaltyGoal = 3,
        TarjetaAmarilla = 4,
        TarjetaRoja = 5,
        Sustitucion = 6,
        Falta = 7,
        InicioPeriodo = 8, // Para marcar cuando empieza el 2T o el Set 3
        FinPeriodo = 9,
        /// <summary>Fuera de juego (fútbol).</summary>
        Offside = 10,
        /// <summary>Tiro total (fútbol; estadística de vitrina).</summary>
        Tiro = 11,
        /// <summary>Tiro a puerta (fútbol).</summary>
        TiroAPuerta = 12,
        /// <summary>Segunda amarilla, normalmente deriva en expulsión.</summary>
        SegundaAmarilla = 13,
        /// <summary>Roja aplicada por doble amarilla.</summary>
        RojaPorDobleAmarilla = 14,
        /// <summary>Penal de tanda no convertido (no suma al marcador de penales).</summary>
        PenaltyMiss = 15,
    }
}
