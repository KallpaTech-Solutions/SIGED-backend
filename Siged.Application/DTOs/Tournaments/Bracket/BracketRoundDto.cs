using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Bracket
{
    public class BracketRoundDto
    {
        public string Title { get; set; } = string.Empty; // "Cuartos", "Semis", etc.
        public List<BracketMatchDto> Matches { get; set; } = new();
    }
}
