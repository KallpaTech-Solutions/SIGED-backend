using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core.Tournaments.Enums
{
    public enum PlayerPosition
    {
        None = 0,
        // Fútbol
        Portero = 1,
        Defensa = 2,
        Mediocampista = 3,
        Delantero = 4,
        // Vóley
        Libero = 5,
        Armador = 6,
        Central = 7,
        // Genérico
        Capitan = 8
    }
}
