using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Group
{
    public class BulkAssignTeamsDto
    {
        [Required]
        public Guid GroupId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debes enviar al menos un equipo.")]
        public List<Guid> TeamIds { get; set; } = new();
    }
}
