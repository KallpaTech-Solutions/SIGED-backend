
namespace Siged.Application.DTOs.Tournaments
{
    public class DisciplineDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
