using Siged.Application.DTOs.Tournaments.Player;


namespace Siged.Application.DTOs.Tournaments.Team
{
    public class TeamDetailsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Initials { get; set; }
        public string? LogoUrl { get; set; }
        public string? RepresentativeName { get; set; }

        // Usamos el PlayerDto que ya creaste para evitar el ciclo
        public List<PlayerDto> Players { get; set; } = new();
    }
}
