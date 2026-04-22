using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Discipline
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty; // Ej: "Fútbol"
        public string? IconUrl { get; set; }
        /// <summary>Clave de plantilla usada al crear (FIFA_FOOTBALL, FIVB_VOLLEYBALL, etc.).</summary>
        public string? TemplateKey { get; set; }
        public bool IsActive { get; set; } = true;
        public ScoringType ScoringType { get; set; }

        // Relación con las reglas (Features)
        public virtual ICollection<DisciplineRule> Rules { get; set; } = new List<DisciplineRule>();
        public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();
    }
}
