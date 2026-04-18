using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Team
{
    public class InscribeTeamDto
    {
        [Required]
        public Guid CompetitionId { get; set; }

        [Required]
        public Guid TeamId { get; set; }
    }
}
