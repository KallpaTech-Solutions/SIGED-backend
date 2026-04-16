
namespace Siged.Application.DTOs.Tournaments.Group
{
    public class GroupResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int QualifiedCount { get; set; }
        public int TeamsCount { get; set; }
    }
}
