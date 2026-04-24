using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Team;

public class AddTeamGestorDto
{
    [Required]
    public int UsuarioId { get; set; }
}
