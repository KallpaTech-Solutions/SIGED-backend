using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Group
{
    public class AssignTeamToGroupDto
    {
        [Required]
        public Guid GroupId { get; set; }

        [Required]
        public Guid TeamId { get; set; }
    }
}
