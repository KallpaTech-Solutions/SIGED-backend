using System.ComponentModel.DataAnnotations;
using Siged.Domain.Entities.Security;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Team
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // --- 🔗 CONEXIÓN CON TU SISTEMA DE ORGANIZACIÓN ---
        public int OrganizacionId { get; set; } // El ID de la Escuela (Hija)
        public virtual Organizacion Organizacion { get; set; } = null!;

        /// <summary>
        /// Usuario considerado creador / responsable de negocio del equipo (p. ej. delegado principal asignado por SuperAdmin).
        /// Equipos antiguos pueden tener null: rige la política legacy por organización.
        /// </summary>
        public int? CreatedByUsuarioId { get; set; }
        public virtual Usuario? CreatedByUsuario { get; set; }
        public string? LogoUrl { get; set; }

        [MaxLength(5)]
        public string? Initials { get; set; } // Ej: "INF", "AGR"

        public string? RepresentativeName { get; set; } // Delegado/Responsable

        public bool IsActive { get; set; } = true; // Para el borrado lógico

        // Relación con Jugadores (Ya la tenías)
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();
        public virtual ICollection<CompetitionTeam> CompetitionTeams { get; set; } = new List<CompetitionTeam>();

        // Relación con Grupos (Para la Fase 3)
        // Esta es la "matrícula" del equipo en los torneos
        public virtual ICollection<GroupTeam> GroupTeams { get; set; } = new List<GroupTeam>();

        public virtual ICollection<TeamGestor> Gestores { get; set; } = new List<TeamGestor>();
    }
}
