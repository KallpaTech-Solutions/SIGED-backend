using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Venue
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Estadio Universitario"

        public string? Address { get; set; }
        public int Capacity { get; set; }
    }
}
