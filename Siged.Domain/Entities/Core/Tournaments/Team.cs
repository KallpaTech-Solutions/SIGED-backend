using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Team
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        [MaxLength(5)]
        public string? Initials { get; set; } // Ej: "INF", "AGR"

        public string? RepresentativeName { get; set; } // Delegado/Responsable

        public bool IsActive { get; set; } = true; // Para el borrado lógico

        // Relación con Jugadores (Ya la tenías)
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();

        // Relación con Grupos (Para la Fase 3)
        // Esta es la "matrícula" del equipo en los torneos
        public virtual ICollection<GroupTeam> GroupTeams { get; set; } = new List<GroupTeam>();
    }
}
