using System;

namespace Siged.Application.DTOs.Tournaments.Match
{
    public class MatchEventDetailDto
    {
        public Guid Id { get; set; }
        public int Minute { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public Guid? PlayerId { get; set; }
        public string? PlayerName { get; set; } // "Benjamín Paz" en lugar de solo el ID
        public Guid? RelatedPlayerId { get; set; }
        public string? RelatedPlayerName { get; set; }
        public string? Note { get; set; }
        public int Value { get; set; }
        public int Period { get; set; }
        public DateTime? OccurredAt { get; set; }
    }
}
