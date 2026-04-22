using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Venue
{
    public class UpdateVenueDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [Range(0, int.MaxValue)]
        public int Capacity { get; set; }

        public Guid? ComplexId { get; set; }
    }
}
