using System.ComponentModel.DataAnnotations;
using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Competition
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Relación con el Torneo (ej: Olimpiadas 2026)
        public Guid TournamentId { get; set; }
        public virtual Tournament Tournament { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        // Relación con el Deporte (ej: Fútbol)
        public Guid DisciplineId { get; set; }
        public virtual Discipline Discipline { get; set; } = null!;

        public Gender Gender { get; set; }
        public string? CategoryName { get; set; } // Ej: "Libre", "Inter-Escuelas"

        // Una competición tiene sus propias fases y partidos
        public virtual ICollection<Phase> Phases { get; set; } = new List<Phase>();

    }
}
