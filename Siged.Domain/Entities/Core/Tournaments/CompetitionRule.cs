using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class CompetitionRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CompetitionId { get; set; }
        public virtual Competition Competition { get; set; } = null!;

        public string RuleKey { get; set; } = string.Empty;   // Ej: "PERIOD_DURATION"
        public string RuleValue { get; set; } = string.Empty; // Ej: "45"
    }
}
