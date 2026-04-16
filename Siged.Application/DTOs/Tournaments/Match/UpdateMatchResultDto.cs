using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Application.DTOs.Tournaments.Match
{
    public class UpdateMatchResultDto
    {
        public int LocalScore { get; set; }
        public int VisitorScore { get; set; }

        // Opcional: Para que la mesa pueda poner comentarios (ej: "W.O." o "Partido suspendido")
        public string? Observations { get; set; }
    }
}
