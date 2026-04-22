using System;

namespace Siged.Application.DTOs.Tournaments.Match
{
    public class MatchEventDetailDto
    {
        public Guid Id { get; set; }
        public int Minute { get; set; }
        public string Type { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string? PlayerName { get; set; } // "Benjamín Paz" en lugar de solo el ID
        public string? Note { get; set; }
        public int Value { get; set; }
        public int Period { get; set; }
        public DateTime? OccurredAt { get; set; }
    }
}
