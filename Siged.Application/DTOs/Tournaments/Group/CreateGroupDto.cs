using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Group
{
    public class CreateGroupDto
    {
        [Required]
        public Guid PhaseId { get; set; }

        [Required, MaxLength(10)]
        public string Name { get; set; } = string.Empty; // "Grupo A"

        public int QualifiedCount { get; set; } = 2; // Cuántos pasan de aquí
    }
}
