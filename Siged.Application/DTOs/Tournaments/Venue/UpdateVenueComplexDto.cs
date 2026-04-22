using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Venue
{
    public class UpdateVenueComplexDto
    {
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

        [MaxLength(500)]
        public string? MapUrl { get; set; }

        [MaxLength(1000)]
        public string? OpeningHoursNote { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
