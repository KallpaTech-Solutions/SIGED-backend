using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class DisciplineRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DisciplineId { get; set; }
        public virtual Discipline Discipline { get; set; } = null!;

        public string RuleKey { get; set; } = string.Empty; // Ej: "TIENE_TARJETAS"
        public string RuleValue { get; set; } = string.Empty; // Ej: "True"
    }
}
