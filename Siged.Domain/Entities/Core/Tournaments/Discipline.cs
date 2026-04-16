using Siged.Domain.Entities.Core.Tournaments.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Discipline
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty; // Ej: "Fútbol"
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;

        // Relación con las reglas (Features)
        public virtual ICollection<DisciplineRule> Rules { get; set; } = new List<DisciplineRule>();
    }
}
