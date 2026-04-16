using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments
{
    public class CreateTournamentDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Year { get; set; }

        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Organizer { get; set; }

        // El archivo del logo que viene desde la laptop/celular
        public IFormFile? LogoFile { get; set; }
    }
}
