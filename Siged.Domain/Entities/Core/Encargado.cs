using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core
{
    public class Encargado : Persona
    {
        public required string Cargo { get; set; } // Ejemplo: "Director de Deportes"
        public string? Oficina { get; set; }
    }
}
