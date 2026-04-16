using Siged.Domain.Entities.Core.Tournaments.Enums;
using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Phase
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CompetitionId { get; set; }
        public virtual Competition Competition { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty; // Ej: "Fase de Grupos"

        public PhaseType Type { get; set; }
        public bool IsDirectElimination { get; set; }
        public int Order { get; set; }

        // Relaciones
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
        public virtual ICollection<Journal> Journals { get; set; } = new List<Journal>();
    }
}
