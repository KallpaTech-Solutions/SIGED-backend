using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Match
{
    public class MatchEventResponseDto
    {
        public Guid Id { get; set; }
        public Guid MatchId { get; set; }
        public int Minute { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid TeamId { get; set; }
        public Guid? PlayerId { get; set; }
        public string? Note { get; set; }
        public int Value { get; set; }
        public int Period { get; set; }
    }
}
