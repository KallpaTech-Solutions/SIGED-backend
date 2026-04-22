using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Venue
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Losa 1", "Campo 11"

        public string? Address { get; set; }
        public int Capacity { get; set; }

        /// <summary>Complejo al que pertenece esta cancha/losa (opcional).</summary>
        public Guid? ComplexId { get; set; }

        public virtual VenueComplex? Complex { get; set; }
    }
}

