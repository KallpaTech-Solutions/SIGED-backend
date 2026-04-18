using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Journal
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid GroupId { get; set; }
        public virtual Group Group { get; set; } = null!;
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty; // Ej: "Fecha 1"

        public int Sequence { get; set; } // El número de la fecha

        public bool IsActive { get; set; } = true;
        public Guid PhaseId { get; set; }
        public virtual Phase Phase { get; set; } = null!;

        public DateTime ScheduledDate { get; set; }
        public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}
