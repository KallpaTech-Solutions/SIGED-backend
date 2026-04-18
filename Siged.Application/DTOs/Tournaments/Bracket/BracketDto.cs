using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Bracket
{
    public class BracketDto
    {
        public Guid PhaseId { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public List<BracketRoundDto> Rounds { get; set; } = new();
    }
}
