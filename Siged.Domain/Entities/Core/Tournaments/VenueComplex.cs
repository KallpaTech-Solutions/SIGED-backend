using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Tournaments
{
    /// <summary>
    /// Complejo deportivo (ej. UNAS): agrupa varias <see cref="Venue"/> (canchas, losas).
    /// Los partidos siguen referenciando la <see cref="Venue"/> concreta, no el complejo.
    /// </summary>
    public class VenueComplex
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? ContactName { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        [MaxLength(200)]
        public string? ContactEmail { get; set; }

        /// <summary>URL a mapa estático, Google Maps, etc.</summary>
        [MaxLength(500)]
        public string? MapUrl { get; set; }

        /// <summary>Texto libre: horarios de atención del complejo.</summary>
        [MaxLength(1000)]
        public string? OpeningHoursNote { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Venue> Venues { get; set; } = new List<Venue>();
    }
}
