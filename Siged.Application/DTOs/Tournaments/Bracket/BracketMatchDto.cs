using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Bracket
{
    public class BracketMatchDto
    {
        public Guid MatchId { get; set; }
        public string? LocalName { get; set; }
        public string? VisitorName { get; set; }
        public int? LocalScore { get; set; }
        public int? VisitorScore { get; set; }
        public int? LocalPenaltyScore { get; set; }
        public int? VisitorPenaltyScore { get; set; }
        public Guid? WinnerId { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }

        public DateTime? ScheduledAt { get; set; }
        public string? VenueName { get; set; }
    }
}
