using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Application.DTOs.Tournaments.Discipline
{
    public class DisciplineDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public ScoringType ScoringType { get; set; } 
    }
}