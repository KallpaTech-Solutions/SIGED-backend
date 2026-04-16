using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core.Tournaments.Enums
{
    public enum PhaseType
    {
        RoundRobin,        // Todos contra todos (Grupos)
        EliminacionSimple, // Knockout
        EliminacionDoble,  // Con ronda de perdedores
        Suizo              // Sistema Suizo (opcional para el futuro)
    }
}
