using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Group
{
    public class GroupDetailsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int QualifiedCount { get; set; }
        public List<TeamSummaryDto> Teams { get; set; } = new();
    }
}
