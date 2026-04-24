using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Playoff
{
    public class PlayoffManualPairingDto
    {
        [Required]
        public Guid LocalTeamId { get; set; }

        [Required]
        public Guid VisitorTeamId { get; set; }
    }
}
