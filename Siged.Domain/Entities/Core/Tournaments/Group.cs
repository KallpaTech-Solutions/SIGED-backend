using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Group
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PhaseId { get; set; }
        public virtual Phase Phase { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int QualifiedCount { get; set; } = 2;

        // --- Propiedades de Navegación (ESTO ES LO QUE FALTA) ---

        // Relación con los equipos inscritos
        public virtual ICollection<GroupTeam> GroupTeams { get; set; } = new List<GroupTeam>();

        // Relación con las jornadas/fechas del fixture
        public virtual ICollection<Journal> Journals { get; set; } = new List<Journal>();
    }
}
