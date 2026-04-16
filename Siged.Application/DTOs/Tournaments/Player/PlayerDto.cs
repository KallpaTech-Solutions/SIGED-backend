using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Application.DTOs.Tournaments.Player
{
    public class PlayerDto
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string? PhotoUrl { get; set; }
        public PlayerPosition Position { get; set; }
        public int? Number { get; set; }
        public bool IsActive { get; set; }
        public bool IsEligible { get; set; }
    }
}
