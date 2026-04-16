using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Team
{
    public class TeamDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Initials { get; set; }
        public string? LogoUrl { get; set; }
        public string? RepresentativeName { get; set; }
        public bool IsActive { get; set; }
    }
}
