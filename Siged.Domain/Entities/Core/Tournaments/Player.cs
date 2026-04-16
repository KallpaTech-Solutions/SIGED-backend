using Siged.Domain.Entities.Core.Tournaments.Enums;
using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Player
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Relación con el Equipo
        [Required]
        public Guid TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(15)] // Aumentamos longitud por si hay pasaportes
        public string Dni { get; set; } = string.Empty;

        public DateTime? BirthDate { get; set; }

        public string? PhotoUrl { get; set; }

        // --- Campos de juego ---

        public int? Number { get; set; } // Dorsal / Camiseta

        public PlayerPosition Position { get; set; } = PlayerPosition.None;

        // --- Estado y Auditoría ---

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ¿Está habilitado para jugar? (Por si debe documentos o está sancionado)
        public bool IsEligible { get; set; } = true;

        // Relación con eventos de partido (Goles, Tarjetas)
        public virtual ICollection<MatchEvent> MatchEvents { get; set; } = new List<MatchEvent>();
    }
}
