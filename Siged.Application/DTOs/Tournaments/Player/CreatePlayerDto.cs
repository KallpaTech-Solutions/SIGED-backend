using Microsoft.AspNetCore.Http;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Player
{
    public class CreatePlayerDto
    {
        [Required]
        public Guid TeamId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Identificador único del jugador: DNI o código de estudiante (ej. 0020180314).</summary>
        [Required, MaxLength(10)]
        public string Dni { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }

        public PlayerPosition? Position { get; set; } // Portero, Defensa, etc.

        public int? Number { get; set; } // Número de camiseta

        public IFormFile? PhotoFile { get; set; }
    }
}
